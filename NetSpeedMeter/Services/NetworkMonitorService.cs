using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    /// <summary>
    /// Continuous live network traffic monitor for Windows 11.
    /// Calculates download and upload speeds using network interface byte counters.
    /// Note: As with all interface-level counters, this measures all traffic passing
    /// through the selected network adapter (including LAN and local file transfers),
    /// not solely public internet traffic.
    /// </summary>
    public class NetworkMonitorService : INetworkMonitorService
    {
        private readonly ISettingsService _settingsService;
        private readonly INetworkAdapterService _adapterService;

        private CancellationTokenSource? _cts;
        private Task? _monitorTask;
        private readonly object _syncLock = new();

        private NetworkInterface? _activeInterface;
        private string _activeInterfaceId = string.Empty;
        private string _activeAdapterDisplayName = "Auto";

        private long _lastBytesReceived = -1;
        private long _lastBytesSent = -1;
        private long _lastTimestamp;
        private bool _resetRequested = true;

        public bool IsMonitoring { get; private set; }
        public NetworkSpeed CurrentSpeed { get; private set; } = NetworkSpeed.Zero();
        public string CurrentAdapterDisplayName => _activeAdapterDisplayName;

        public event Action<NetworkSpeed>? SpeedUpdated;
        public event Action<bool>? MonitoringStateChanged;
        public event Action<string>? ActiveAdapterChanged;

        public NetworkMonitorService(ISettingsService settingsService, INetworkAdapterService adapterService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _adapterService = adapterService ?? throw new ArgumentNullException(nameof(adapterService));

            // Register system network and power event handlers
            NetworkChange.NetworkAddressChanged += OnNetworkChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            _settingsService.SettingsChanged += OnSettingsChanged;

            ReevaluateAdapter();
        }

        public void Start()
        {
            lock (_syncLock)
            {
                if (IsMonitoring)
                {
                    return;
                }

                IsMonitoring = true;
                _resetRequested = true;
                _cts = new CancellationTokenSource();
                _monitorTask = Task.Run(() => MonitoringLoopAsync(_cts.Token));
            }

            MonitoringStateChanged?.Invoke(true);
        }

        public void Stop()
        {
            CancellationTokenSource? ctsToCancel;
            Task? taskToWait;

            lock (_syncLock)
            {
                if (!IsMonitoring)
                {
                    return;
                }

                IsMonitoring = false;
                ctsToCancel = _cts;
                taskToWait = _monitorTask;
                _cts = null;
                _monitorTask = null;
            }

            try
            {
                ctsToCancel?.Cancel();
                taskToWait?.Wait(500);
            }
            catch
            {
                // Task cancellation exceptions are expected
            }
            finally
            {
                ctsToCancel?.Dispose();
            }

            CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
            SpeedUpdated?.Invoke(CurrentSpeed);
            MonitoringStateChanged?.Invoke(false);
        }

        public void ResetBaseline()
        {
            lock (_syncLock)
            {
                _resetRequested = true;
                _lastBytesReceived = -1;
                _lastBytesSent = -1;
            }
        }

        public void ReevaluateAdapter()
        {
            lock (_syncLock)
            {
                string targetId = _settingsService.Settings.SelectedAdapterId;
                NetworkInterface? ni = _adapterService.FindInterface(targetId);

                _activeInterface = ni;
                _activeInterfaceId = targetId;

                if (ni != null)
                {
                    _activeAdapterDisplayName = targetId == "auto"
                        ? $"{ni.Name} (Auto-detected)"
                        : ni.Name;
                }
                else
                {
                    _activeAdapterDisplayName = "No Active Connection";
                }

                ResetBaseline();
            }

            ActiveAdapterChanged?.Invoke(_activeAdapterDisplayName);
        }

        private async Task MonitoringLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    SampleNetworkTraffic();

                    double intervalSeconds = Math.Max(0.5, Math.Min(10.0, _settingsService.Settings.RefreshIntervalSeconds));
                    int delayMs = (int)(intervalSeconds * 1000);

                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NetworkMonitor] Error in monitoring loop: {ex.Message}");
                    ResetBaseline();
                    try
                    {
                        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private void SampleNetworkTraffic()
        {
            lock (_syncLock)
            {
                // Ensure active interface is valid
                if (_activeInterface == null || _activeInterface.OperationalStatus != OperationalStatus.Up)
                {
                    // Attempt to re-find active interface if in auto mode
                    if (_activeInterfaceId == "auto" || string.IsNullOrEmpty(_activeInterfaceId))
                    {
                        _activeInterface = _adapterService.FindBestActiveInterface();
                        if (_activeInterface != null)
                        {
                            _activeAdapterDisplayName = $"{_activeInterface.Name} (Auto-detected)";
                            ActiveAdapterChanged?.Invoke(_activeAdapterDisplayName);
                        }
                    }
                }

                if (_activeInterface == null || _activeInterface.OperationalStatus != OperationalStatus.Up)
                {
                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                    return;
                }

                // Query current interface statistics
                long currentRx = 0;
                long currentTx = 0;

                try
                {
                    var ipStats = _activeInterface.GetIPStatistics();
                    currentRx = ipStats.BytesReceived;
                    currentTx = ipStats.BytesSent;

                    // Fallback to IPv4 statistics if GetIPStatistics returned 0 and IPv4 is supported
                    if (currentRx == 0 && currentTx == 0 && _activeInterface.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        var ipv4Stats = _activeInterface.GetIPv4Statistics();
                        currentRx = ipv4Stats.BytesReceived;
                        currentTx = ipv4Stats.BytesSent;
                    }
                }
                catch
                {
                    // If interface was reset or removed, re-evaluate next tick
                    _resetRequested = true;
                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                    return;
                }

                long currentTimestamp = Stopwatch.GetTimestamp();

                // If baseline reset was requested, or this is first sample, record and show 0
                if (_resetRequested || _lastBytesReceived < 0 || _lastBytesSent < 0)
                {
                    _lastBytesReceived = currentRx;
                    _lastBytesSent = currentTx;
                    _lastTimestamp = currentTimestamp;
                    _resetRequested = false;

                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                    return;
                }

                // Handle counter wrap or interface resets
                if (currentRx < _lastBytesReceived || currentTx < _lastBytesSent)
                {
                    _lastBytesReceived = currentRx;
                    _lastBytesSent = currentTx;
                    _lastTimestamp = currentTimestamp;

                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                    return;
                }

                double elapsedSeconds = (double)(currentTimestamp - _lastTimestamp) / Stopwatch.Frequency;
                if (elapsedSeconds <= 0.05)
                {
                    // Avoid division by tiny delta
                    return;
                }

                long deltaRx = currentRx - _lastBytesReceived;
                long deltaTx = currentTx - _lastBytesSent;

                _lastBytesReceived = currentRx;
                _lastBytesSent = currentTx;
                _lastTimestamp = currentTimestamp;

                double downloadSpeed = Math.Max(0.0, deltaRx / elapsedSeconds);
                double uploadSpeed = Math.Max(0.0, deltaTx / elapsedSeconds);

                var speedUnit = _settingsService.Settings.SpeedUnit;
                string dlFormatted = SpeedFormatter.Format(downloadSpeed, speedUnit);
                string ulFormatted = SpeedFormatter.Format(uploadSpeed, speedUnit);

                CurrentSpeed = new NetworkSpeed
                {
                    DownloadBytesPerSec = downloadSpeed,
                    UploadBytesPerSec = uploadSpeed,
                    DownloadFormatted = dlFormatted,
                    UploadFormatted = ulFormatted,
                    AdapterName = _activeAdapterDisplayName,
                    RawBytesReceived = currentRx,
                    RawBytesSent = currentTx,
                    Timestamp = DateTime.UtcNow
                };

                SpeedUpdated?.Invoke(CurrentSpeed);
            }
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            // If the user picked a different adapter, switch and reset baseline
            if (settings.SelectedAdapterId != _activeInterfaceId)
            {
                ReevaluateAdapter();
            }
        }

        private void OnNetworkChanged(object? sender, EventArgs e)
        {
            // System network route / IP address changed
            ReevaluateAdapter();
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            // Network cable plugged in or unplugged / Wi-Fi disconnected
            ReevaluateAdapter();
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                // When resuming from sleep or hibernation, reset baseline to prevent false speed spikes
                ResetBaseline();
                ReevaluateAdapter();
            }
        }

        public void Dispose()
        {
            Stop();

            NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            _settingsService.SettingsChanged -= OnSettingsChanged;

            _cts?.Dispose();
        }
    }
}

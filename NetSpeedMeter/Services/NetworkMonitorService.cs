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
        private readonly SemaphoreSlim _wakeSignal = new(0, 1);

        private NetworkInterface? _activeInterface;
        private string _activeInterfaceId = string.Empty;
        private string _activeAdapterDisplayName = "Auto";

        private long _lastBytesReceived = -1;
        private long _lastBytesSent = -1;
        private long _lastTimestamp;
        private bool _resetRequested = true;
        private int _periodicCheckCounter;

        private readonly SpeedSmoother _downloadSmoother = new();
        private readonly SpeedSmoother _uploadSmoother = new();

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

            ReevaluateAdapter(isSeamless: false);
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
                WakeMonitoringLoop();
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
                _downloadSmoother.Reset();
                _uploadSmoother.Reset();
            }

            WakeMonitoringLoop();
        }

        private void WakeMonitoringLoop()
        {
            try
            {
                if (_wakeSignal.CurrentCount == 0)
                {
                    _wakeSignal.Release();
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void SwitchAdapter(NetworkInterface newInterface, bool isSeamless)
        {
            lock (_syncLock)
            {
                if (_activeInterface != null &&
                    _activeInterface.Id == newInterface.Id &&
                    _activeInterface.OperationalStatus == OperationalStatus.Up)
                {
                    return;
                }

                _activeInterface = newInterface;
                string targetId = _settingsService.Settings.SelectedAdapterId;
                _activeAdapterDisplayName = (targetId == "auto" || string.IsNullOrEmpty(targetId))
                    ? $"{newInterface.Name} (Auto-detected)"
                    : newInterface.Name;

                try
                {
                    var ipStats = newInterface.GetIPStatistics();
                    long rx = ipStats.BytesReceived;
                    long tx = ipStats.BytesSent;
                    if (rx == 0 && tx == 0 && newInterface.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        var ipv4Stats = newInterface.GetIPv4Statistics();
                        rx = ipv4Stats.BytesReceived;
                        tx = ipv4Stats.BytesSent;
                    }

                    _lastBytesReceived = rx;
                    _lastBytesSent = tx;
                    _lastTimestamp = Stopwatch.GetTimestamp();
                    _resetRequested = false;
                }
                catch
                {
                    _resetRequested = true;
                }

                if (!isSeamless)
                {
                    _downloadSmoother.Reset();
                    _uploadSmoother.Reset();
                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                }
            }

            ActiveAdapterChanged?.Invoke(_activeAdapterDisplayName);
            WakeMonitoringLoop();
        }

        public void ReevaluateAdapter(bool isSeamless = false)
        {
            string targetId;
            lock (_syncLock)
            {
                targetId = _settingsService.Settings.SelectedAdapterId;
                _activeInterfaceId = targetId;
            }

            NetworkInterface? ni = _adapterService.FindInterface(targetId);

            if (ni != null)
            {
                SwitchAdapter(ni, isSeamless);
            }
            else
            {
                lock (_syncLock)
                {
                    _activeInterface = null;
                    _activeAdapterDisplayName = "No Active Connection";
                    _lastBytesReceived = -1;
                    _lastBytesSent = -1;
                    _resetRequested = true;
                    _downloadSmoother.Reset();
                    _uploadSmoother.Reset();
                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                }

                ActiveAdapterChanged?.Invoke(_activeAdapterDisplayName);
            }
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

                    bool wasWoken = await _wakeSignal.WaitAsync(delayMs, cancellationToken).ConfigureAwait(false);
                    if (wasWoken)
                    {
                        // Immediate adapter transition: pause 150ms to accumulate byte counters on new adapter, then sample right away
                        await Task.Delay(150, cancellationToken).ConfigureAwait(false);
                    }
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
                // In auto mode, check if current adapter went down OR if a higher priority adapter became available
                bool isAutoMode = _activeInterfaceId == "auto" || string.IsNullOrEmpty(_activeInterfaceId);
                bool adapterNeedsCheck = _activeInterface == null ||
                                         _activeInterface.OperationalStatus != OperationalStatus.Up ||
                                         (isAutoMode && ++_periodicCheckCounter % 3 == 0);

                if (isAutoMode && adapterNeedsCheck)
                {
                    var bestInterface = _adapterService.FindBestActiveInterface();
                    if (bestInterface != null && (_activeInterface == null || _activeInterface.Id != bestInterface.Id || _activeInterface.OperationalStatus != OperationalStatus.Up))
                    {
                        // Seamlessly transition to the newly available / superior adapter
                        SwitchAdapter(bestInterface, isSeamless: true);
                        return;
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
                    // Interface query failed (e.g. disconnected) - re-evaluate next tick
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

                double elapsedSeconds = (double)(currentTimestamp - _lastTimestamp) / Stopwatch.Frequency;
                if (elapsedSeconds <= 0.05)
                {
                    // Avoid division by tiny delta
                    return;
                }

                // If elapsed time is unusually large (e.g. system suspended, sleep wake, or thread freeze),
                // re-synchronize baseline immediately without calculating across the sleep gap to prevent false spikes.
                double intervalSeconds = Math.Max(0.5, Math.Min(10.0, _settingsService.Settings.RefreshIntervalSeconds));
                if (elapsedSeconds > Math.Max(5.0, intervalSeconds * 3.0))
                {
                    _lastBytesReceived = currentRx;
                    _lastBytesSent = currentTx;
                    _lastTimestamp = currentTimestamp;
                    _resetRequested = false;
                    _downloadSmoother.Reset();
                    _uploadSmoother.Reset();

                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                    return;
                }

                // Calculate delta with 32-bit network driver counter rollover support
                bool rxValid = TryCalculateDelta(currentRx, _lastBytesReceived, elapsedSeconds, out long deltaRx);
                bool txValid = TryCalculateDelta(currentTx, _lastBytesSent, elapsedSeconds, out long deltaTx);

                _lastBytesReceived = currentRx;
                _lastBytesSent = currentTx;
                _lastTimestamp = currentTimestamp;

                if (!rxValid || !txValid)
                {
                    // Interface reset or unrecoverable counter jump - baseline reset
                    CurrentSpeed = NetworkSpeed.Zero(_activeAdapterDisplayName);
                    SpeedUpdated?.Invoke(CurrentSpeed);
                    return;
                }

                double rawDl = Math.Max(0.0, deltaRx / elapsedSeconds);
                double rawUl = Math.Max(0.0, deltaTx / elapsedSeconds);

                double downloadSpeed = _settingsService.Settings.EnableSpeedSmoothing
                    ? _downloadSmoother.Smooth(rawDl)
                    : rawDl;
                double uploadSpeed = _settingsService.Settings.EnableSpeedSmoothing
                    ? _uploadSmoother.Smooth(rawUl)
                    : rawUl;

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

        /// <summary>
        /// Calculates byte delta between successive samples, handling 32-bit counter rollovers
        /// commonly seen in Windows network adapter drivers (wrapping at ~4.29 GB).
        /// </summary>
        internal static bool TryCalculateDelta(long current, long last, double elapsedSeconds, out long delta)
        {
            if (current >= last)
            {
                delta = current - last;
                return true;
            }

            // Check if this is a 32-bit counter rollover
            if (last <= uint.MaxValue && current <= uint.MaxValue)
            {
                long wrappedDelta = ((long)uint.MaxValue - last) + current + 1;
                if (wrappedDelta >= 0 && elapsedSeconds > 0)
                {
                    double bytesPerSec = wrappedDelta / elapsedSeconds;
                    const double MaxPlausibleRate = 12.5 * 1024 * 1024 * 1024; // 100 Gbps sanity threshold
                    if (bytesPerSec <= MaxPlausibleRate)
                    {
                        delta = wrappedDelta;
                        return true;
                    }
                }
            }

            // Interface was reset or counter wrapped unexpectedly
            delta = 0;
            return false;
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            // If the user picked a different adapter, switch and reset baseline
            if (settings.SelectedAdapterId != _activeInterfaceId)
            {
                ReevaluateAdapter(isSeamless: false);
            }
        }

        private void OnNetworkChanged(object? sender, EventArgs e)
        {
            // System network route / IP address changed
            ReevaluateAdapter(isSeamless: true);
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            // Network cable plugged in or unplugged / Wi-Fi disconnected
            ReevaluateAdapter(isSeamless: true);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                ResetBaseline();
            }
            else if (e.Mode == PowerModes.Resume)
            {
                // When resuming from sleep or hibernation, reset baseline to prevent false speed spikes
                ResetBaseline();
                ReevaluateAdapter(isSeamless: false);
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
            _wakeSignal.Dispose();
        }
    }
}

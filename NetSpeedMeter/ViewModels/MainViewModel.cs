using System;
using System.Windows.Input;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INetworkMonitorService _monitorService;

        private string _downloadSpeed = "0 B/s";
        private string _uploadSpeed = "0 B/s";
        private bool _isMonitoring = true;
        private string _activeAdapterName = "Detecting adapter...";
        private bool _isMeterVisible = true;
        private bool _isDarkTheme = true;
        private SpeedDisplayMode _displayMode = SpeedDisplayMode.Both;

        public bool ShowDownload => _displayMode == SpeedDisplayMode.Both || _displayMode == SpeedDisplayMode.DownloadOnly;
        public bool ShowUpload => _displayMode == SpeedDisplayMode.Both || _displayMode == SpeedDisplayMode.UploadOnly;

        public string DownloadSpeed
        {
            get => _downloadSpeed;
            set => SetProperty(ref _downloadSpeed, value);
        }

        public string UploadSpeed
        {
            get => _uploadSpeed;
            set => SetProperty(ref _uploadSpeed, value);
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                if (SetProperty(ref _isMonitoring, value))
                {
                    OnPropertyChanged(nameof(MonitoringStatusText));
                    OnPropertyChanged(nameof(ToggleMonitoringButtonText));
                }
            }
        }

        public string MonitoringStatusText => IsMonitoring ? "Monitoring: ON" : "Monitoring: OFF";
        public string ToggleMonitoringButtonText => IsMonitoring ? "Stop Monitoring" : "Start Monitoring";

        public string ActiveAdapterName
        {
            get => _activeAdapterName;
            set => SetProperty(ref _activeAdapterName, value);
        }

        public bool IsMeterVisible
        {
            get => _isMeterVisible;
            set
            {
                if (SetProperty(ref _isMeterVisible, value))
                {
                    OnPropertyChanged(nameof(ToggleMeterButtonText));
                }
            }
        }

        public string ToggleMeterButtonText => IsMeterVisible ? "Hide Taskbar-Area Meter" : "Show Taskbar-Area Meter";

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public ICommand ToggleMonitoringCommand { get; }
        public ICommand ToggleMeterCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand MinimizeToTrayCommand { get; }

        public MainViewModel(
            ISettingsService settingsService,
            INetworkMonitorService monitorService,
            Action toggleMeter,
            Action openSettings,
            Action minimizeToTray)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));

            _isMonitoring = _monitorService.IsMonitoring;
            _activeAdapterName = _monitorService.CurrentAdapterDisplayName;
            _isMeterVisible = _settingsService.Settings.IsMeterVisible;
            _isDarkTheme = _settingsService.Settings.Theme == AppTheme.Dark;
            _displayMode = _settingsService.Settings.DisplayMode;

            ToggleMonitoringCommand = new RelayCommand(() =>
            {
                if (_monitorService.IsMonitoring)
                {
                    _monitorService.Stop();
                }
                else
                {
                    _monitorService.Start();
                }
            });

            ToggleMeterCommand = new RelayCommand(toggleMeter);
            OpenSettingsCommand = new RelayCommand(openSettings);
            MinimizeToTrayCommand = new RelayCommand(minimizeToTray);

            _monitorService.SpeedUpdated += OnSpeedUpdated;
            _monitorService.MonitoringStateChanged += OnMonitoringStateChanged;
            _monitorService.ActiveAdapterChanged += OnActiveAdapterChanged;
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        private void OnSpeedUpdated(NetworkSpeed speed)
        {
            DownloadSpeed = speed.DownloadFormatted;
            UploadSpeed = speed.UploadFormatted;
        }

        private void OnMonitoringStateChanged(bool isMonitoring)
        {
            IsMonitoring = isMonitoring;
        }

        private void OnActiveAdapterChanged(string adapterName)
        {
            ActiveAdapterName = adapterName;
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            IsMeterVisible = settings.IsMeterVisible;
            IsDarkTheme = settings.Theme == AppTheme.Dark;
            _displayMode = settings.DisplayMode;
            OnPropertyChanged(nameof(ShowDownload));
            OnPropertyChanged(nameof(ShowUpload));
        }
    }
}

using System;
using System.Windows.Input;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.ViewModels
{
    public class SpeedMeterViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INetworkMonitorService _monitorService;

        private string _downloadSpeedText = "0 B/s";
        private string _uploadSpeedText = "0 B/s";
        private bool _isLocked;
        private double _opacity = 0.92;
        private bool _isDarkTheme = true;
        private bool _isMonitoring = true;
        private SpeedDisplayMode _displayMode = SpeedDisplayMode.Both;
        private double _fontSize = 12.0;
        private MeterLayoutStyle _layoutStyle = MeterLayoutStyle.TaskbarClockStacked;

        public MeterLayoutStyle LayoutStyle
        {
            get => _layoutStyle;
            set
            {
                if (SetProperty(ref _layoutStyle, value))
                {
                    OnPropertyChanged(nameof(IsClockStackedStyle));
                    OnPropertyChanged(nameof(IsHorizontalPillStyle));
                }
            }
        }

        public bool IsClockStackedStyle => LayoutStyle == MeterLayoutStyle.TaskbarClockStacked;
        public bool IsHorizontalPillStyle => LayoutStyle == MeterLayoutStyle.HorizontalPill;

        public double FontSize
        {
            get => _fontSize;
            set
            {
                if (SetProperty(ref _fontSize, value))
                {
                    OnPropertyChanged(nameof(ArrowFontSize));
                    OnPropertyChanged(nameof(StackedFontSize));
                    OnPropertyChanged(nameof(StackedArrowFontSize));
                }
            }
        }

        public double ArrowFontSize => Math.Max(10, FontSize + 1.5);
        public double StackedFontSize => Math.Max(9.0, Math.Min(14.0, FontSize - 1.5));
        public double StackedArrowFontSize => Math.Max(9.0, StackedFontSize + 0.5);

        public SpeedDisplayMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (SetProperty(ref _displayMode, value))
                {
                    OnPropertyChanged(nameof(ShowDownload));
                    OnPropertyChanged(nameof(ShowUpload));
                    OnPropertyChanged(nameof(ShowSeparator));
                }
            }
        }

        public bool ShowDownload => DisplayMode == SpeedDisplayMode.Both || DisplayMode == SpeedDisplayMode.DownloadOnly;
        public bool ShowUpload => DisplayMode == SpeedDisplayMode.Both || DisplayMode == SpeedDisplayMode.UploadOnly;
        public bool ShowSeparator => DisplayMode == SpeedDisplayMode.Both;

        public string DownloadSpeedText
        {
            get => _downloadSpeedText;
            set => SetProperty(ref _downloadSpeedText, value);
        }

        public string UploadSpeedText
        {
            get => _uploadSpeedText;
            set => SetProperty(ref _uploadSpeedText, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (SetProperty(ref _isLocked, value))
                {
                    _settingsService.Update(s => s.IsMeterLocked = value);
                    OnPropertyChanged(nameof(LockStatusText));
                }
            }
        }

        public string LockStatusText => IsLocked ? "Unlock Position" : "Lock Position";

        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                if (SetProperty(ref _isMonitoring, value))
                {
                    OnPropertyChanged(nameof(MonitoringToggleText));
                }
            }
        }

        public string MonitoringToggleText => IsMonitoring ? "Stop Monitoring" : "Start Monitoring";

        public ICommand OpenMainWindowCommand { get; }
        public ICommand ToggleMeterCommand { get; }
        public ICommand ToggleLockCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ToggleMonitoringCommand { get; }
        public ICommand ResetPositionCommand { get; }
        public ICommand ExitCommand { get; }

        public SpeedMeterViewModel(
            ISettingsService settingsService,
            INetworkMonitorService monitorService,
            Action openMainWindow,
            Action toggleMeter,
            Action openSettings,
            Action exitApp,
            Action? resetPosition = null)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));

            _isLocked = _settingsService.Settings.IsMeterLocked;
            _opacity = _settingsService.Settings.MeterOpacity;
            _isDarkTheme = ThemeHelper.ResolveIsDark(_settingsService.Settings.Theme);
            _isMonitoring = _monitorService.IsMonitoring;
            _displayMode = _settingsService.Settings.DisplayMode;
            _fontSize = _settingsService.Settings.MeterFontSize;
            _layoutStyle = _settingsService.Settings.LayoutStyle;

            OpenMainWindowCommand = new RelayCommand(openMainWindow);
            ToggleMeterCommand = new RelayCommand(toggleMeter);
            ToggleLockCommand = new RelayCommand(() => IsLocked = !IsLocked);
            OpenSettingsCommand = new RelayCommand(openSettings);
            ResetPositionCommand = new RelayCommand(() => resetPosition?.Invoke());
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
            ExitCommand = new RelayCommand(exitApp);

            _monitorService.SpeedUpdated += OnSpeedUpdated;
            _monitorService.MonitoringStateChanged += OnMonitoringStateChanged;
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        public void RefreshSystemTheme()
        {
            if (_settingsService.Settings.Theme == AppTheme.System)
            {
                IsDarkTheme = ThemeHelper.IsWindowsTaskbarDark();
            }
        }

        private void OnSpeedUpdated(NetworkSpeed speed)
        {
            DownloadSpeedText = speed.DownloadFormatted;
            UploadSpeedText = speed.UploadFormatted;
        }

        private void OnMonitoringStateChanged(bool isMonitoring)
        {
            IsMonitoring = isMonitoring;
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            IsLocked = settings.IsMeterLocked;
            Opacity = settings.MeterOpacity;
            IsDarkTheme = ThemeHelper.ResolveIsDark(settings.Theme);
            DisplayMode = settings.DisplayMode;
            FontSize = settings.MeterFontSize;
            LayoutStyle = settings.LayoutStyle;
        }
    }
}

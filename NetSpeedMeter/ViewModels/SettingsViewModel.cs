using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly INetworkAdapterService _adapterService;
        private readonly IStartupService _startupService;
        private readonly Action _resetPositionAction;
        private readonly Action _closeWindowAction;

        private bool _launchAtStartup;
        private bool _autoStartMonitoring;
        private bool _autoShowFloatingMeter;
        private bool _minimizeToTrayOnClose;
        private double _refreshIntervalSeconds;
        private NetworkAdapterInfo? _selectedAdapter;
        private SpeedUnitMode _selectedSpeedUnit;
        private AppTheme _selectedTheme;
        private double _meterOpacity;
        private double _meterFontSize;
        private bool _isMeterLocked;
        private SpeedDisplayMode _selectedDisplayMode;
        private MeterDockPosition _selectedDockPosition;
        private MeterLayoutStyle _selectedLayoutStyle;

        public ObservableCollection<NetworkAdapterInfo> AvailableAdapters { get; } = new();

        public SpeedDisplayMode SelectedDisplayMode
        {
            get => _selectedDisplayMode;
            set => SetProperty(ref _selectedDisplayMode, value);
        }

        public MeterLayoutStyle SelectedLayoutStyle
        {
            get => _selectedLayoutStyle;
            set => SetProperty(ref _selectedLayoutStyle, value);
        }

        public MeterDockPosition SelectedDockPosition
        {
            get => _selectedDockPosition;
            set => SetProperty(ref _selectedDockPosition, value);
        }

        public bool LaunchAtStartup
        {
            get => _launchAtStartup;
            set => SetProperty(ref _launchAtStartup, value);
        }

        public bool AutoStartMonitoring
        {
            get => _autoStartMonitoring;
            set => SetProperty(ref _autoStartMonitoring, value);
        }

        public bool AutoShowFloatingMeter
        {
            get => _autoShowFloatingMeter;
            set => SetProperty(ref _autoShowFloatingMeter, value);
        }

        public bool MinimizeToTrayOnClose
        {
            get => _minimizeToTrayOnClose;
            set => SetProperty(ref _minimizeToTrayOnClose, value);
        }

        public double RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set => SetProperty(ref _refreshIntervalSeconds, value);
        }

        public NetworkAdapterInfo? SelectedAdapter
        {
            get => _selectedAdapter;
            set => SetProperty(ref _selectedAdapter, value);
        }

        public SpeedUnitMode SelectedSpeedUnit
        {
            get => _selectedSpeedUnit;
            set => SetProperty(ref _selectedSpeedUnit, value);
        }

        public AppTheme SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        public double MeterOpacity
        {
            get => _meterOpacity;
            set => SetProperty(ref _meterOpacity, value);
        }

        public double MeterFontSize
        {
            get => _meterFontSize;
            set => SetProperty(ref _meterFontSize, value);
        }

        public bool IsMeterLocked
        {
            get => _isMeterLocked;
            set => SetProperty(ref _isMeterLocked, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetPositionCommand { get; }
        public ICommand RestoreDefaultsCommand { get; }

        public SettingsViewModel(
            ISettingsService settingsService,
            INetworkAdapterService adapterService,
            IStartupService startupService,
            Action resetPositionAction,
            Action closeWindowAction)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _adapterService = adapterService ?? throw new ArgumentNullException(nameof(adapterService));
            _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
            _resetPositionAction = resetPositionAction ?? throw new ArgumentNullException(nameof(resetPositionAction));
            _closeWindowAction = closeWindowAction ?? throw new ArgumentNullException(nameof(closeWindowAction));

            LoadCurrentSettings();

            SaveCommand = new RelayCommand(SaveSettings);
            CancelCommand = new RelayCommand(_closeWindowAction);
            ResetPositionCommand = new RelayCommand(_resetPositionAction);
            RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        }

        private void LoadCurrentSettings()
        {
            var s = _settingsService.Settings;

            _launchAtStartup = _startupService.IsStartupEnabled();
            _autoStartMonitoring = s.AutoStartMonitoring;
            _autoShowFloatingMeter = s.AutoShowFloatingMeter;
            _minimizeToTrayOnClose = s.MinimizeToTrayOnClose;
            _refreshIntervalSeconds = s.RefreshIntervalSeconds;
            _selectedSpeedUnit = s.SpeedUnit;
            _selectedTheme = s.Theme;
            _meterOpacity = s.MeterOpacity;
            _meterFontSize = s.MeterFontSize;
            _isMeterLocked = s.IsMeterLocked;
            _selectedDisplayMode = s.DisplayMode;
            _selectedDockPosition = s.DockPosition;
            _selectedLayoutStyle = s.LayoutStyle;

            AvailableAdapters.Clear();
            var adapters = _adapterService.GetAvailableAdapters();
            foreach (var adapter in adapters)
            {
                AvailableAdapters.Add(adapter);
            }

            _selectedAdapter = AvailableAdapters.FirstOrDefault(a => a.Id == s.SelectedAdapterId)
                               ?? AvailableAdapters.FirstOrDefault();

            OnPropertyChanged(string.Empty);
        }

        private void SaveSettings()
        {
            _startupService.SetStartup(LaunchAtStartup);

            _settingsService.Update(s =>
            {
                if (s.DockPosition != SelectedDockPosition || s.LayoutStyle != SelectedLayoutStyle)
                {
                    // Reset custom position so it immediately snaps and centers with new dimensions
                    s.MeterCustomLeft = null;
                    s.MeterCustomTop = null;
                }

                s.LaunchAtStartup = LaunchAtStartup;
                s.AutoStartMonitoring = AutoStartMonitoring;
                s.AutoShowFloatingMeter = AutoShowFloatingMeter;
                s.MinimizeToTrayOnClose = MinimizeToTrayOnClose;
                s.RefreshIntervalSeconds = RefreshIntervalSeconds;
                s.SelectedAdapterId = SelectedAdapter?.Id ?? "auto";
                s.SpeedUnit = SelectedSpeedUnit;
                s.Theme = SelectedTheme;
                s.MeterOpacity = MeterOpacity;
                s.MeterFontSize = MeterFontSize;
                s.IsMeterLocked = IsMeterLocked;
                s.DisplayMode = SelectedDisplayMode;
                s.DockPosition = SelectedDockPosition;
                s.LayoutStyle = SelectedLayoutStyle;
            });

            _closeWindowAction();
        }

        private void RestoreDefaults()
        {
            _settingsService.ResetToDefaults();
            LoadCurrentSettings();
        }
    }
}

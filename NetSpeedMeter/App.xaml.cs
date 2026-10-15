using System;
using System.Threading;
using System.Windows;
using NetSpeedMeter.Models;
using NetSpeedMeter.Services;
using NetSpeedMeter.ViewModels;
using NetSpeedMeter.Views;

namespace NetSpeedMeter
{
    public partial class App : Application
    {
        private const string MutexName = "Local\\NetSpeedMeter_SingleInstance_Mutex";
        private Mutex? _singleInstanceMutex;

        private ISettingsService? _settingsService;
        private IStartupService? _startupService;
        private INetworkAdapterService? _adapterService;
        private INetworkMonitorService? _monitorService;
        private IMeterPositionService? _positionService;
        private ITrayIconService? _trayService;

        private MainWindow? _mainWindow;
        private SpeedMeterWindow? _meterWindow;
        private SettingsWindow? _settingsWindow;

        private MainViewModel? _mainViewModel;
        private SpeedMeterViewModel? _meterViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                MessageBox.Show(
                    "NetSpeed Meter is already running. Check your taskbar and notification area.",
                    "NetSpeed Meter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetSpeedMeter");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"), $"[{DateTime.Now}] Dispatcher exception: {args.Exception}\n");
                }
                catch { }
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                try
                {
                    string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetSpeedMeter");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"), $"[{DateTime.Now}] AppDomain exception: {args.ExceptionObject}\n");
                }
                catch { }
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                try
                {
                    string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetSpeedMeter");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"), $"[{DateTime.Now}] Task exception: {args.Exception}\n");
                }
                catch { }
            };

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetSpeedMeter");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "app.log"), $"[{DateTime.Now}] ProcessExit triggered.\nStackTrace: {Environment.StackTrace}\n");
                }
                catch { }
            };

            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetSpeedMeter");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "app.log"), $"[{DateTime.Now}] App started successfully.\n");
            }
            catch { }

            InitializeApplication();
        }

        private void InitializeApplication()
        {
            _settingsService = new SettingsService();
            _startupService = new StartupService();
            _adapterService = new NetworkAdapterService();
            _monitorService = new NetworkMonitorService(_settingsService, _adapterService);
            _positionService = new MeterPositionService(_settingsService);
            _trayService = new TrayIconService(_settingsService, _startupService);

            _mainViewModel = new MainViewModel(
                _settingsService,
                _monitorService,
                ToggleFloatingMeter,
                OpenSettings,
                MinimizeToTray);

            _meterViewModel = new SpeedMeterViewModel(
                _settingsService,
                _monitorService,
                RestoreMainWindow,
                ToggleFloatingMeter,
                OpenSettings,
                ExitApplication,
                ResetMeterPosition);

            _mainWindow = new MainWindow(_mainViewModel, _settingsService);
            MainWindow = _mainWindow;

            _meterWindow = new SpeedMeterWindow(
                _meterViewModel,
                _positionService,
                RestoreMainWindow);

            _settingsService.SettingsChanged += settings =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (_meterWindow != null && _positionService != null)
                    {
                        if (!settings.MeterCustomLeft.HasValue || !settings.MeterCustomTop.HasValue)
                        {
                            _positionService.ApplySavedOrCalculatedPosition(_meterWindow);
                        }
                    }
                });
            };

            _trayService.Initialize(
                RestoreMainWindow,
                ToggleFloatingMeter,
                ToggleMonitoring,
                OpenSettings,
                ExitApplication);

            _monitorService.SpeedUpdated += speed =>
            {
                Dispatcher.InvokeAsync(() => _trayService.UpdateSpeed(speed));
            };

            _monitorService.MonitoringStateChanged += isMonitoring =>
            {
                Dispatcher.InvokeAsync(() => _trayService.UpdateMonitoringState(isMonitoring));
            };

            if (_settingsService.Settings.AutoStartMonitoring)
            {
                _monitorService.Start();
            }

            _meterWindow.Show();
            _settingsService.Update(s => s.IsMeterVisible = true);
            _trayService.UpdateMeterVisibility(true);

            bool startMinimized = Environment.GetCommandLineArgs().Length > 1 &&
                                  string.Equals(Environment.GetCommandLineArgs()[1], "--minimized", StringComparison.OrdinalIgnoreCase);

            if (!startMinimized)
            {
                _mainWindow.Show();
            }
        }

        public void RestoreMainWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (_mainWindow == null)
                {
                    return;
                }

                _mainWindow.RestoreAndActivate();
            });
        }

        public void ToggleFloatingMeter()
        {
            Dispatcher.Invoke(() =>
            {
                if (_meterWindow == null || _settingsService == null || _trayService == null)
                {
                    return;
                }

                bool currentlyVisible = _meterWindow.IsVisible;
                bool newVisibleState = !currentlyVisible;

                if (newVisibleState)
                {
                    _meterWindow.Show();
                    _positionService?.ApplySavedOrCalculatedPosition(_meterWindow);
                }
                else
                {
                    _meterWindow.Hide();
                }

                _settingsService.Update(s => s.IsMeterVisible = newVisibleState);
                _trayService.UpdateMeterVisibility(newVisibleState);
            });
        }

        public void ToggleMonitoring()
        {
            if (_monitorService == null)
            {
                return;
            }

            if (_monitorService.IsMonitoring)
            {
                _monitorService.Stop();
            }
            else
            {
                _monitorService.Start();
            }
        }

        public void OpenSettings()
        {
            Dispatcher.Invoke(() =>
            {
                if (_settingsWindow != null && _settingsWindow.IsVisible)
                {
                    _settingsWindow.Activate();
                    return;
                }

                if (_settingsService == null || _adapterService == null || _startupService == null || _positionService == null)
                {
                    return;
                }

                var vm = new SettingsViewModel(
                    _settingsService,
                    _adapterService,
                    _startupService,
                    ResetMeterPosition,
                    CloseSettings);

                _settingsWindow = new SettingsWindow(vm)
                {
                    Owner = _mainWindow?.IsVisible == true ? _mainWindow : null
                };

                _settingsWindow.ShowDialog();
            });
        }

        private void ResetMeterPosition()
        {
            Dispatcher.Invoke(() =>
            {
                if (_meterWindow != null && _positionService != null)
                {
                    _positionService.ResetToTaskbarPosition(_meterWindow);
                }
            });
        }

        private void CloseSettings()
        {
            Dispatcher.Invoke(() =>
            {
                _settingsWindow?.Close();
                _settingsWindow = null;
            });
        }

        public void MinimizeToTray()
        {
            Dispatcher.Invoke(() =>
            {
                _mainWindow?.Hide();
            });
        }

        public void ExitApplication()
        {
            Dispatcher.Invoke(() =>
            {
                _mainWindow?.ForceClose();
                _meterWindow?.Close();
                _settingsWindow?.Close();

                _trayService?.Dispose();
                _monitorService?.Dispose();

                Shutdown();
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayService?.Dispose();
            _monitorService?.Dispose();

            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch
                {
                }
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }

            base.OnExit(e);
        }
    }
}

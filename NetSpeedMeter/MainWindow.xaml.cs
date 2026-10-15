using System;
using System.ComponentModel;
using System.Windows;
using NetSpeedMeter.Services;
using NetSpeedMeter.ViewModels;

namespace NetSpeedMeter
{
    public partial class MainWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private bool _allowRealClose;

        public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public void ForceClose()
        {
            _allowRealClose = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_allowRealClose && _settingsService.Settings.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnClosing(e);
        }

        public void RestoreAndActivate()
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Topmost = true;
            Topmost = false;
            Activate();
            Focus();
        }
    }
}

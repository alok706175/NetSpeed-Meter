using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Services;
using NetSpeedMeter.ViewModels;
using Point = System.Windows.Point;

namespace NetSpeedMeter.Views
{
    public partial class SpeedMeterWindow : Window
    {
        private readonly IMeterPositionService _positionService;
        private readonly Action _openMainWindowAction;

        private uint _taskbarCreatedMsg;
        private HwndSource? _hwndSource;
        private readonly System.Windows.Threading.DispatcherTimer _topmostTimer;

        public SpeedMeterViewModel ViewModel => (SpeedMeterViewModel)DataContext;

        public SpeedMeterWindow(
            SpeedMeterViewModel viewModel,
            IMeterPositionService positionService,
            Action openMainWindowAction)
        {
            InitializeComponent();
            DataContext = viewModel;
            _positionService = positionService ?? throw new ArgumentNullException(nameof(positionService));
            _openMainWindowAction = openMainWindowAction ?? throw new ArgumentNullException(nameof(openMainWindowAction));

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            SourceInitialized += OnSourceInitialized;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            _topmostTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _topmostTimer.Tick += (s, e) => EnsureTopmost();
            _topmostTimer.Start();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _positionService.ApplySavedOrCalculatedPosition(this);
            Dispatcher.InvokeAsync(() => _positionService.ApplySavedOrCalculatedPosition(this), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _positionService.ApplySavedOrCalculatedPosition(this);
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            IntPtr currentExStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
            long newExStyle = currentExStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newExStyle));

            _taskbarCreatedMsg = NativeMethods.RegisterWindowMessage("TaskbarCreated");

            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(HwndMessageHook);

            EnsureTopmost();
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DPICHANGED = 0x02E0;

            if (_taskbarCreatedMsg != 0 && (uint)msg == _taskbarCreatedMsg)
            {
                Dispatcher.InvokeAsync(() => _positionService.ApplySavedOrCalculatedPosition(this));
            }
            else if (msg == WM_DPICHANGED)
            {
                Dispatcher.InvokeAsync(() => _positionService.ApplySavedOrCalculatedPosition(this));
            }

            return IntPtr.Zero;
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() => _positionService.ApplySavedOrCalculatedPosition(this));
        }
        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            try
            {
                _openMainWindowAction();
            }
            catch
            {
            }
            EnsureTopmost();
        }

        public void EnsureTopmost()
        {
            if (_hwndSource != null && _hwndSource.Handle != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(
                    _hwndSource.Handle,
                    NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _topmostTimer.Stop();
            SizeChanged -= OnSizeChanged;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _hwndSource?.RemoveHook(HwndMessageHook);
            base.OnClosed(e);
        }
    }
}

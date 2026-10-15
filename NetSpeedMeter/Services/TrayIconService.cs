using System;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    public class TrayIconService : ITrayIconService
    {
        private readonly ISettingsService _settingsService;
        private readonly IStartupService _startupService;

        private WinForms.NotifyIcon? _notifyIcon;
        private WinForms.ContextMenuStrip? _contextMenu;
        private WinForms.ToolStripMenuItem? _openMenuItem;
        private WinForms.ToolStripMenuItem? _toggleMeterMenuItem;
        private WinForms.ToolStripMenuItem? _toggleMonitoringMenuItem;
        private WinForms.ToolStripMenuItem? _settingsMenuItem;
        private WinForms.ToolStripMenuItem? _startupMenuItem;
        private WinForms.ToolStripMenuItem? _exitMenuItem;

        private Action? _openMainWindowAction;
        private Action? _toggleMeterAction;
        private Action? _toggleMonitoringAction;
        private Action? _openSettingsAction;
        private Action? _exitAppAction;

        private Drawing.Icon? _createdIcon;
        private string _lastTooltip = string.Empty;

        public TrayIconService(ISettingsService settingsService, IStartupService startupService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        }

        public void Initialize(Action openMainWindow, Action toggleMeter, Action toggleMonitoring, Action openSettings, Action exitApp)
        {
            _openMainWindowAction = openMainWindow;
            _toggleMeterAction = toggleMeter;
            _toggleMonitoringAction = toggleMonitoring;
            _openSettingsAction = openSettings;
            _exitAppAction = exitApp;

            _contextMenu = new WinForms.ContextMenuStrip();

            _openMenuItem = new WinForms.ToolStripMenuItem("Open NetSpeed Meter", null, (_, _) => _openMainWindowAction?.Invoke())
            {
                Font = new Drawing.Font("Segoe UI", 9F, Drawing.FontStyle.Bold)
            };

            _toggleMeterMenuItem = new WinForms.ToolStripMenuItem("Floating Meter", null, (_, _) => _toggleMeterAction?.Invoke())
            {
                Checked = _settingsService.Settings.IsMeterVisible
            };

            _toggleMonitoringMenuItem = new WinForms.ToolStripMenuItem("Start/Stop Monitoring", null, (_, _) => _toggleMonitoringAction?.Invoke())
            {
                Checked = _settingsService.Settings.AutoStartMonitoring
            };

            _settingsMenuItem = new WinForms.ToolStripMenuItem("Settings...", null, (_, _) => _openSettingsAction?.Invoke());

            _startupMenuItem = new WinForms.ToolStripMenuItem("Start with Windows", null, (_, _) =>
            {
                bool newState = !_startupMenuItem!.Checked;
                if (_startupService.SetStartup(newState))
                {
                    _startupMenuItem.Checked = newState;
                    _settingsService.Update(s => s.LaunchAtStartup = newState);
                }
            })
            {
                Checked = _startupService.IsStartupEnabled()
            };

            _exitMenuItem = new WinForms.ToolStripMenuItem("Exit", null, (_, _) => _exitAppAction?.Invoke());

            _contextMenu.Items.AddRange(new WinForms.ToolStripItem[]
            {
                _openMenuItem,
                new WinForms.ToolStripSeparator(),
                _toggleMonitoringMenuItem,
                _settingsMenuItem,
                _startupMenuItem,
                new WinForms.ToolStripSeparator(),
                _exitMenuItem
            });

            _createdIcon = GenerateTrayIcon();

            _notifyIcon = new WinForms.NotifyIcon
            {
                Icon = _createdIcon,
                Text = "NetSpeed Meter",
                Visible = true,
                ContextMenuStrip = _contextMenu
            };

            _notifyIcon.DoubleClick += (_, _) => _openMainWindowAction?.Invoke();
        }

        public void UpdateSpeed(NetworkSpeed speed)
        {
            if (_notifyIcon == null)
            {
                return;
            }

            try
            {
                string speedLine = _settingsService.Settings.DisplayMode switch
                {
                    SpeedDisplayMode.DownloadOnly => $"↓ {speed.DownloadFormatted}",
                    SpeedDisplayMode.UploadOnly => $"↑ {speed.UploadFormatted}",
                    _ => $"↓ {speed.DownloadFormatted}  ↑ {speed.UploadFormatted}"
                };

                // Windows notify icon text is limited to 63 chars (or 127 in newer APIs).
                string tooltip = $"NetSpeed Meter\n{speedLine}";
                if (tooltip.Length >= 64)
                {
                    tooltip = speedLine;
                }

                if (tooltip.Length >= 64)
                {
                    tooltip = tooltip.Substring(0, 63);
                }

                if (string.Equals(_lastTooltip, tooltip, StringComparison.Ordinal))
                {
                    return;
                }

                _lastTooltip = tooltip;
                _notifyIcon.Text = tooltip;
            }
            catch
            {
                // Avoid tooltip crash
            }
        }

        public void UpdateMonitoringState(bool isMonitoring)
        {
            if (_toggleMonitoringMenuItem != null)
            {
                _toggleMonitoringMenuItem.Text = isMonitoring ? "Monitoring: Active (Stop)" : "Monitoring: Stopped (Start)";
                _toggleMonitoringMenuItem.Checked = isMonitoring;
            }
        }

        public void UpdateMeterVisibility(bool isVisible)
        {
            if (_toggleMeterMenuItem != null)
            {
                _toggleMeterMenuItem.Checked = isVisible;
                _toggleMeterMenuItem.Text = isVisible ? "Hide Floating Meter" : "Show Floating Meter";
            }
        }

        private static Drawing.Icon GenerateTrayIcon()
        {
            try
            {
                // First try loading app.ico from local disk Assets folder
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string iconPath = System.IO.Path.Combine(appDir, "Assets", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    return new Drawing.Icon(iconPath, 32, 32);
                }

                // Next try loading embedded resource
                var resourceUri = new Uri("pack://application:,,,/Assets/app.ico");
                var streamInfo = System.Windows.Application.GetResourceStream(resourceUri);
                if (streamInfo?.Stream != null)
                {
                    using var s = streamInfo.Stream;
                    return new Drawing.Icon(s, 32, 32);
                }
            }
            catch
            {
                // Fallback to programmatic rendering if resource loading fails
            }

            using var bitmap = new Drawing.Bitmap(32, 32);
            using var g = Drawing.Graphics.FromImage(bitmap);
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);

            // Dark rounded circular pill background
            using (var bgBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(235, 18, 22, 28)))
            {
                g.FillEllipse(bgBrush, 1, 1, 30, 30);
            }

            // Outer border
            using (var borderPen = new Drawing.Pen(Drawing.Color.FromArgb(160, 0, 162, 255), 1.5f))
            {
                g.DrawEllipse(borderPen, 1, 1, 30, 30);
            }

            // Down arrow (Download - vibrant cyan #00D1FF)
            using (var dlBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 0, 209, 255)))
            {
                // Stem
                g.FillRectangle(dlBrush, 9, 8, 4, 9);
                // Arrow head
                var arrowPoints = new Drawing.Point[]
                {
                    new(5, 17),
                    new(17, 17),
                    new(11, 23)
                };
                g.FillPolygon(dlBrush, arrowPoints);
            }

            // Up arrow (Upload - vibrant orange/amber #FF9900)
            using (var ulBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 255, 153, 0)))
            {
                // Stem
                g.FillRectangle(ulBrush, 19, 15, 4, 9);
                // Arrow head
                var arrowPoints = new Drawing.Point[]
                {
                    new(15, 15),
                    new(27, 15),
                    new(21, 9)
                };
                g.FillPolygon(ulBrush, arrowPoints);
            }

            IntPtr hIcon = bitmap.GetHicon();
            return Drawing.Icon.FromHandle(hIcon);
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _contextMenu?.Dispose();
            _contextMenu = null;

            _createdIcon?.Dispose();
            _createdIcon = null;
        }
    }
}

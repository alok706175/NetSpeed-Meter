using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace NetSpeedMeter.Services
{
    public class MeterPositionService : IMeterPositionService
    {
        private readonly ISettingsService _settingsService;

        public MeterPositionService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public Point CalculateDefaultPosition(double windowWidth, double windowHeight)
        {
            Rect workArea = SystemParameters.WorkArea;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            double taskbarLeft;
            double taskbarTop;
            double taskbarWidth;
            double taskbarHeight;

            // Determine taskbar position and dimensions in native WPF DIPs directly from WorkArea
            if (workArea.Bottom < screenHeight)
            {
                // Standard bottom taskbar (Windows 11 default)
                taskbarLeft = workArea.Left;
                taskbarTop = workArea.Bottom;
                taskbarWidth = workArea.Width;
                taskbarHeight = screenHeight - workArea.Bottom;
            }
            else if (workArea.Top > 0)
            {
                // Top taskbar
                taskbarLeft = workArea.Left;
                taskbarTop = 0;
                taskbarWidth = workArea.Width;
                taskbarHeight = workArea.Top;
            }
            else if (workArea.Left > 0)
            {
                // Left taskbar
                taskbarLeft = 0;
                taskbarTop = workArea.Top;
                taskbarWidth = workArea.Left;
                taskbarHeight = workArea.Height;
            }
            else if (workArea.Right < screenWidth)
            {
                // Right taskbar
                taskbarLeft = workArea.Right;
                taskbarTop = workArea.Top;
                taskbarWidth = screenWidth - workArea.Right;
                taskbarHeight = workArea.Height;
            }
            else
            {
                // Auto-hidden taskbar or fallback
                taskbarLeft = 0;
                taskbarHeight = 48.0;
                taskbarTop = screenHeight - taskbarHeight;
                taskbarWidth = screenWidth;
            }

            if (taskbarHeight < 24.0)
            {
                taskbarHeight = 48.0;
                taskbarTop = screenHeight - taskbarHeight;
            }

            var dockPos = _settingsService.Settings.DockPosition;

            if (dockPos == MeterDockPosition.TaskbarLeft)
            {
                // Extreme left edge of taskbar (mirrors system clock on far right)
                double leftX = taskbarLeft + 6.0;
                double centerY = taskbarTop + Math.Max(0, (taskbarHeight - windowHeight) / 2.0);
                return new Point(leftX, centerY);
            }
            else
            {
                const double margin = 8.0;
                const double trayOffset = 180.0;
                double rightX = Math.Max(margin, (taskbarLeft + taskbarWidth) - windowWidth - trayOffset);
                double centerY = taskbarTop + Math.Max(0, (taskbarHeight - windowHeight) / 2.0);
                return new Point(rightX, centerY);
            }
        }

        public void ApplySavedOrCalculatedPosition(Window window)
        {
            double width = window.ActualWidth > 0 ? window.ActualWidth : 80;
            double height = window.ActualHeight > 0 ? window.ActualHeight : 30;

            var settings = _settingsService.Settings;

            // When locked, always anchor to the fixed taskbar position (like the Windows clock)
            if (!settings.IsMeterLocked && settings.MeterCustomLeft.HasValue && settings.MeterCustomTop.HasValue)
            {
                double savedLeft = settings.MeterCustomLeft.Value;
                double savedTop = settings.MeterCustomTop.Value;

                // Validate that the saved position is still within visible virtual screen boundaries
                if (IsPositionVisibleOnScreen(savedLeft, savedTop, width, height))
                {
                    window.Left = savedLeft;
                    window.Top = savedTop;
                    return;
                }
            }

            // Calculate default position near the Windows 11 taskbar
            Point defaultPos = CalculateDefaultPosition(width, height);
            window.Left = defaultPos.X;
            window.Top = defaultPos.Y;
        }

        public void SavePosition(Window window)
        {
            if (window.WindowState != WindowState.Normal)
            {
                return;
            }

            _settingsService.Update(s =>
            {
                s.MeterCustomLeft = window.Left;
                s.MeterCustomTop = window.Top;
            });
        }

        public void ResetToTaskbarPosition(Window window)
        {
            _settingsService.Update(s =>
            {
                s.MeterCustomLeft = null;
                s.MeterCustomTop = null;
                s.IsMeterLocked = true;
            });

            double width = window.ActualWidth > 0 ? window.ActualWidth : 80;
            double height = window.ActualHeight > 0 ? window.ActualHeight : 30;
            Point defaultPos = CalculateDefaultPosition(width, height);

            window.Left = defaultPos.X;
            window.Top = defaultPos.Y;
        }

        private static bool IsPositionVisibleOnScreen(double left, double top, double width, double height)
        {
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
            double virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

            // Ensure at least 30% of the window is within screen bounds
            return left + width * 0.3 >= virtualLeft &&
                   left + width * 0.7 <= virtualRight &&
                   top + height * 0.3 >= virtualTop &&
                   top + height * 0.7 <= virtualBottom;
        }
    }
}

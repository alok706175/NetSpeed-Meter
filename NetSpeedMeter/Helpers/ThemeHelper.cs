using System;
using Microsoft.Win32;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Helpers
{
    public static class ThemeHelper
    {
        private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string SystemUsesLightThemeValue = "SystemUsesLightTheme";

        /// <summary>
        /// Reads Windows 11 / 10 taskbar theme from Registry:
        /// HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize -> SystemUsesLightTheme
        /// Returns true if Taskbar is Dark (0), or false if Taskbar is Light (1).
        /// Defaults to true (Dark) if registry key is not accessible.
        /// </summary>
        public static bool IsWindowsTaskbarDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
                if (key != null)
                {
                    object? val = key.GetValue(SystemUsesLightThemeValue);
                    if (val is int lightThemeInt)
                    {
                        return lightThemeInt == 0;
                    }
                }
            }
            catch
            {
            }

            return true;
        }

        /// <summary>
        /// Resolves whether the taskbar meter should render in Dark or Light mode.
        /// If AppTheme.System is chosen, it automatically queries the Windows 11 taskbar theme.
        /// </summary>
        public static bool ResolveIsDark(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                _ => IsWindowsTaskbarDark()
            };
        }
    }
}

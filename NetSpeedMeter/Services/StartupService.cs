using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace NetSpeedMeter.Services
{
    public interface IStartupService
    {
        bool IsStartupEnabled();
        bool SetStartup(bool enable);
    }

    public class StartupService : IStartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "NetSpeedMeter";

        public bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public bool SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null)
                {
                    return false;
                }

                if (enable)
                {
                    string? exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        return true;
                    }
                    return false;
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

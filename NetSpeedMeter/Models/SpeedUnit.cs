namespace NetSpeedMeter.Models
{
    public enum SpeedUnitMode
    {
        AutoBinary,   // 1024-based (KiB/s, MiB/s, labeled as KB/s, MB/s)
        AutoDecimal,  // 1000-based (KB/s, MB/s, GB/s)
        ForceKB,      // Always display in KB/s
        ForceMB,      // Always display in MB/s
        ForceGB       // Always display in GB/s
    }

    public enum SpeedDisplayMode
    {
        Both,          // Display both Download and Upload speed
        DownloadOnly,  // Display only Download speed
        UploadOnly     // Display only Upload speed
    }

    public enum MeterDockPosition
    {
        TaskbarLeft,   // Far left of taskbar (Windows 11 left area, next to Start)
        TaskbarRight   // Far right of taskbar (near system tray)
    }

    public enum AppTheme
    {
        Dark,
        Light
    }

    public enum MeterLayoutStyle
    {
        TaskbarClockStacked, // 2 lines vertically stacked (like Windows Taskbar Time & Date)
        HorizontalPill       // 1 line horizontal pill
    }
}

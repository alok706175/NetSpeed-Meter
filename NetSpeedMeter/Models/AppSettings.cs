using System;

namespace NetSpeedMeter.Models
{
    public class AppSettings
    {
        public bool LaunchAtStartup { get; set; } = true;
        public bool AutoStartMonitoring { get; set; } = true;
        public bool AutoShowFloatingMeter { get; set; } = true;
        public bool MinimizeToTrayOnClose { get; set; } = true;
        public double RefreshIntervalSeconds { get; set; } = 1.0;
        public string SelectedAdapterId { get; set; } = "auto";
        public SpeedUnitMode SpeedUnit { get; set; } = SpeedUnitMode.AutoBinary;
        public AppTheme Theme { get; set; } = AppTheme.Dark;
        public double MeterOpacity { get; set; } = 0.92;
        public bool IsMeterLocked { get; set; } = true;
        public double? MeterCustomLeft { get; set; } = null;
        public double? MeterCustomTop { get; set; } = null;
        public bool IsMeterVisible { get; set; } = true;
        public SpeedDisplayMode DisplayMode { get; set; } = SpeedDisplayMode.Both;
        public double MeterFontSize { get; set; } = 12.0;
        public MeterDockPosition DockPosition { get; set; } = MeterDockPosition.TaskbarLeft;
        public MeterLayoutStyle LayoutStyle { get; set; } = MeterLayoutStyle.HorizontalPill;

        public AppSettings Clone() => (AppSettings)MemberwiseClone();
    }
}

using System;
using System.Globalization;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Helpers
{
    public static class SpeedFormatter
    {
        public static string Format(double bytesPerSecond, SpeedUnitMode mode)
        {
            if (bytesPerSecond <= 0 || double.IsNaN(bytesPerSecond) || double.IsInfinity(bytesPerSecond))
            {
                return mode switch
                {
                    SpeedUnitMode.ForceKB => "0.0 KB/s",
                    SpeedUnitMode.ForceMB => "0.00 MB/s",
                    SpeedUnitMode.ForceGB => "0.000 GB/s",
                    _ => "0 B/s"
                };
            }

            double baseUnit = mode == SpeedUnitMode.AutoDecimal ? 1000.0 : 1024.0;

            switch (mode)
            {
                case SpeedUnitMode.ForceKB:
                    return string.Format(CultureInfo.InvariantCulture, "{0:F1} KB/s", bytesPerSecond / baseUnit);

                case SpeedUnitMode.ForceMB:
                    return string.Format(CultureInfo.InvariantCulture, "{0:F2} MB/s", bytesPerSecond / (baseUnit * baseUnit));

                case SpeedUnitMode.ForceGB:
                    return string.Format(CultureInfo.InvariantCulture, "{0:F3} GB/s", bytesPerSecond / (baseUnit * baseUnit * baseUnit));

                case SpeedUnitMode.AutoDecimal:
                case SpeedUnitMode.AutoBinary:
                default:
                    if (bytesPerSecond < baseUnit)
                    {
                        return string.Format(CultureInfo.InvariantCulture, "{0:F0} B/s", bytesPerSecond);
                    }
                    else if (bytesPerSecond < baseUnit * baseUnit)
                    {
                        double kb = bytesPerSecond / baseUnit;
                        return kb >= 100
                            ? string.Format(CultureInfo.InvariantCulture, "{0:F0} KB/s", kb)
                            : string.Format(CultureInfo.InvariantCulture, "{0:F1} KB/s", kb);
                    }
                    else if (bytesPerSecond < baseUnit * baseUnit * baseUnit)
                    {
                        double mb = bytesPerSecond / (baseUnit * baseUnit);
                        return string.Format(CultureInfo.InvariantCulture, "{0:F2} MB/s", mb);
                    }
                    else
                    {
                        double gb = bytesPerSecond / (baseUnit * baseUnit * baseUnit);
                        return string.Format(CultureInfo.InvariantCulture, "{0:F2} GB/s", gb);
                    }
            }
        }
    }
}

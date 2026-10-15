using System;
using System.Globalization;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Helpers
{
    public static class SpeedFormatter
    {
        private const string ZeroB = "0 B/s";
        private const string ZeroKB = "0.0 KB/s";
        private const string ZeroMB = "0.00 MB/s";
        private const string ZeroGB = "0.000 GB/s";

        private const string SuffixB = " B/s";
        private const string SuffixKB = " KB/s";
        private const string SuffixMB = " MB/s";
        private const string SuffixGB = " GB/s";

        public static string Format(double bytesPerSecond, SpeedUnitMode mode)
        {
            if (bytesPerSecond <= 0 || double.IsNaN(bytesPerSecond) || double.IsInfinity(bytesPerSecond))
            {
                return mode switch
                {
                    SpeedUnitMode.ForceKB => ZeroKB,
                    SpeedUnitMode.ForceMB => ZeroMB,
                    SpeedUnitMode.ForceGB => ZeroGB,
                    _ => ZeroB
                };
            }

            double baseUnit = mode == SpeedUnitMode.AutoDecimal ? 1000.0 : 1024.0;

            double valueToFormat;
            string format;
            string suffix;

            switch (mode)
            {
                case SpeedUnitMode.ForceKB:
                    valueToFormat = bytesPerSecond / baseUnit;
                    format = "F1";
                    suffix = SuffixKB;
                    break;

                case SpeedUnitMode.ForceMB:
                    valueToFormat = bytesPerSecond / (baseUnit * baseUnit);
                    format = "F2";
                    suffix = SuffixMB;
                    break;

                case SpeedUnitMode.ForceGB:
                    valueToFormat = bytesPerSecond / (baseUnit * baseUnit * baseUnit);
                    format = "F3";
                    suffix = SuffixGB;
                    break;

                case SpeedUnitMode.AutoDecimal:
                case SpeedUnitMode.AutoBinary:
                default:
                    if (bytesPerSecond < baseUnit)
                    {
                        valueToFormat = bytesPerSecond;
                        format = "F0";
                        suffix = SuffixB;
                    }
                    else if (bytesPerSecond < baseUnit * baseUnit)
                    {
                        double kb = bytesPerSecond / baseUnit;
                        valueToFormat = kb;
                        format = kb >= 100 ? "F0" : "F1";
                        suffix = SuffixKB;
                    }
                    else if (bytesPerSecond < baseUnit * baseUnit * baseUnit)
                    {
                        valueToFormat = bytesPerSecond / (baseUnit * baseUnit);
                        format = "F2";
                        suffix = SuffixMB;
                    }
                    else
                    {
                        valueToFormat = bytesPerSecond / (baseUnit * baseUnit * baseUnit);
                        format = "F2";
                        suffix = SuffixGB;
                    }
                    break;
            }

            // Zero-allocation buffer on the stack (no GC heap allocations)
            Span<char> buffer = stackalloc char[32];
            if (valueToFormat.TryFormat(buffer, out int charsWritten, format, CultureInfo.InvariantCulture))
            {
                suffix.AsSpan().CopyTo(buffer.Slice(charsWritten));
                int totalLength = charsWritten + suffix.Length;
                return new string(buffer.Slice(0, totalLength));
            }

            // Fallback (buffer safeguard)
            return string.Create(CultureInfo.InvariantCulture, $"{valueToFormat.ToString(format, CultureInfo.InvariantCulture)}{suffix}");
        }
    }
}

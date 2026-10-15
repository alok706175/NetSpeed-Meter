using System;

namespace NetSpeedMeter.Models
{
    public class NetworkSpeed
    {
        public double DownloadBytesPerSec { get; init; }
        public double UploadBytesPerSec { get; init; }
        public string DownloadFormatted { get; init; } = "0 B/s";
        public string UploadFormatted { get; init; } = "0 B/s";
        public string AdapterName { get; init; } = "Unknown";
        public long RawBytesReceived { get; init; }
        public long RawBytesSent { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public string MeterFormatted => $"↓ {DownloadFormatted}   ↑ {UploadFormatted}";

        public static NetworkSpeed Zero(string adapterName = "Disconnected") => new()
        {
            DownloadBytesPerSec = 0,
            UploadBytesPerSec = 0,
            DownloadFormatted = "0 B/s",
            UploadFormatted = "0 B/s",
            AdapterName = adapterName,
            Timestamp = DateTime.UtcNow
        };
    }
}

using System;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    public interface INetworkMonitorService : IDisposable
    {
        bool IsMonitoring { get; }
        NetworkSpeed CurrentSpeed { get; }
        string CurrentAdapterDisplayName { get; }

        event Action<NetworkSpeed>? SpeedUpdated;
        event Action<bool>? MonitoringStateChanged;
        event Action<string>? ActiveAdapterChanged;

        void Start();
        void Stop();
        void ResetBaseline();
        void ReevaluateAdapter(bool isSeamless = false);
    }
}

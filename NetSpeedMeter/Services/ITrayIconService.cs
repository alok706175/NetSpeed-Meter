using System;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    public interface ITrayIconService : IDisposable
    {
        void Initialize(Action openMainWindow, Action toggleMeter, Action toggleMonitoring, Action openSettings, Action exitApp);
        void UpdateSpeed(NetworkSpeed speed);
        void UpdateMonitoringState(bool isMonitoring);
        void UpdateMeterVisibility(bool isVisible);
    }
}

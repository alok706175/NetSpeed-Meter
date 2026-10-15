using System;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        event Action<AppSettings>? SettingsChanged;
        void Save();
        void Update(Action<AppSettings> updateAction);
        void ResetToDefaults();
    }
}

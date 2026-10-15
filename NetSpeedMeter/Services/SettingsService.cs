using System;
using System.IO;
using System.Text.Json;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Services
{
    public class SettingsService : ISettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _settingsFilePath;
        private readonly object _lock = new();

        public AppSettings Settings { get; private set; }

        public event Action<AppSettings>? SettingsChanged;

        public SettingsService(string? customPath = null)
        {
            if (string.IsNullOrWhiteSpace(customPath))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string directory = Path.Combine(appData, "NetSpeedMeter");
                _settingsFilePath = Path.Combine(directory, "settings.json");
            }
            else
            {
                _settingsFilePath = customPath;
            }

            Settings = LoadSettings();
        }

        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(_settingsFilePath)!;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string json = JsonSerializer.Serialize(Settings, JsonOptions);
                    string tempFile = _settingsFilePath + ".tmp";
                    File.WriteAllText(tempFile, json);

                    if (File.Exists(_settingsFilePath))
                    {
                        File.Replace(tempFile, _settingsFilePath, null);
                    }
                    else
                    {
                        File.Move(tempFile, _settingsFilePath);
                    }
                }
                catch
                {
                    // Fallback direct write if atomic replace encounters an issue
                    try
                    {
                        string json = JsonSerializer.Serialize(Settings, JsonOptions);
                        File.WriteAllText(_settingsFilePath, json);
                    }
                    catch
                    {
                        // Ignore persistence failure to prevent crashing
                    }
                }
            }

            SettingsChanged?.Invoke(Settings);
        }

        public void Update(Action<AppSettings> updateAction)
        {
            updateAction(Settings);
            Save();
        }

        public void ResetToDefaults()
        {
            Settings = new AppSettings();
            Save();
        }

        private AppSettings LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        string json = File.ReadAllText(_settingsFilePath);
                        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                        if (settings != null)
                        {
                            return settings;
                        }
                    }
                }
                catch
                {
                    // Fall back to default settings on error/corruption
                }
            }

            return new AppSettings();
        }
    }
}

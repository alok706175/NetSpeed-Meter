using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Models;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class SettingsServiceTests
    {
        private string _tempSettingsFile = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempSettingsFile = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid():N}.json");
        }

        [TestCleanup]
        public void Teardown()
        {
            if (File.Exists(_tempSettingsFile))
            {
                File.Delete(_tempSettingsFile);
            }
        }

        [TestMethod]
        public void SettingsService_InitializesDefaults_WhenFileDoesNotExist()
        {
            var service = new SettingsService(_tempSettingsFile);

            Assert.IsNotNull(service.Settings);
            Assert.IsTrue(service.Settings.AutoStartMonitoring);
            Assert.IsTrue(service.Settings.AutoShowFloatingMeter);
            Assert.IsTrue(service.Settings.MinimizeToTrayOnClose);
            Assert.AreEqual(1.0, service.Settings.RefreshIntervalSeconds);
            Assert.AreEqual("auto", service.Settings.SelectedAdapterId);
            Assert.AreEqual(SpeedUnitMode.AutoBinary, service.Settings.SpeedUnit);
        }

        [TestMethod]
        public void SettingsService_SavesAndReloads_Correctly()
        {
            var service = new SettingsService(_tempSettingsFile);

            service.Update(s =>
            {
                s.AutoStartMonitoring = false;
                s.RefreshIntervalSeconds = 2.5;
                s.SpeedUnit = SpeedUnitMode.ForceMB;
                s.MeterOpacity = 0.75;
                s.MeterFontSize = 16.0;
                s.IsMeterLocked = true;
                s.DisplayMode = SpeedDisplayMode.DownloadOnly;
                s.DockPosition = MeterDockPosition.TaskbarRight;
            });

            // Read with a new service instance to verify persistence
            var reloadedService = new SettingsService(_tempSettingsFile);
            Assert.IsFalse(reloadedService.Settings.AutoStartMonitoring);
            Assert.AreEqual(2.5, reloadedService.Settings.RefreshIntervalSeconds);
            Assert.AreEqual(SpeedDisplayMode.DownloadOnly, reloadedService.Settings.DisplayMode);
            Assert.AreEqual(MeterDockPosition.TaskbarRight, reloadedService.Settings.DockPosition);
            Assert.AreEqual(SpeedUnitMode.ForceMB, reloadedService.Settings.SpeedUnit);
            Assert.AreEqual(0.75, reloadedService.Settings.MeterOpacity);
            Assert.AreEqual(16.0, reloadedService.Settings.MeterFontSize);
            Assert.IsTrue(reloadedService.Settings.IsMeterLocked);
        }

        [TestMethod]
        public void SettingsService_ResetToDefaults_RestoresInitialValues()
        {
            var service = new SettingsService(_tempSettingsFile);

            service.Update(s =>
            {
                s.RefreshIntervalSeconds = 5.0;
                s.Theme = AppTheme.Light;
            });

            service.ResetToDefaults();

            Assert.AreEqual(1.0, service.Settings.RefreshIntervalSeconds);
            Assert.AreEqual(AppTheme.System, service.Settings.Theme);
        }
    }
}

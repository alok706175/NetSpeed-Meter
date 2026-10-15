using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Models;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class MeterPositionServiceTests
    {
        [TestMethod]
        public void CalculateDefaultPosition_TaskbarLeft_ReturnsVisibleTaskbarCoordinates()
        {
            var settingsService = new SettingsService();
            settingsService.Update(s => s.DockPosition = MeterDockPosition.TaskbarLeft);

            var positionService = new MeterPositionService(settingsService);
            Point pos = positionService.CalculateDefaultPosition(80, 32);

            // X must be anchored near the left edge
            Assert.IsTrue(pos.X >= 0, $"Expected pos.X >= 0, got {pos.X}");

            // Y must be on-screen (not off the bottom of the screen)
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            if (screenHeight > 0)
            {
                Assert.IsTrue(pos.Y < screenHeight, $"pos.Y ({pos.Y}) was placed beyond the bottom of screen ({screenHeight})");
                Assert.IsTrue(pos.Y >= 0, $"pos.Y ({pos.Y}) was placed above screen");
            }
        }
    }
}

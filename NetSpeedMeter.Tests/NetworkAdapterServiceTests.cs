using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class NetworkAdapterServiceTests
    {
        [TestMethod]
        public void GetAvailableAdapters_AlwaysContainsAutomaticOption()
        {
            var service = new NetworkAdapterService();
            var adapters = service.GetAvailableAdapters();

            Assert.IsNotNull(adapters);
            Assert.IsTrue(adapters.Count >= 1);
            Assert.IsTrue(adapters[0].IsAutomatic);
            Assert.AreEqual("auto", adapters[0].Id);
        }

        [TestMethod]
        public void FindInterface_WithAuto_ReturnsBestActiveOrNull()
        {
            var service = new NetworkAdapterService();
            var iface = service.FindInterface("auto");

            // On a connected system, this should find the active adapter (e.g. Wi-Fi)
            // Even if offline, it should handle safely without throwing
            if (iface != null)
            {
                Assert.IsFalse(string.IsNullOrEmpty(iface.Name));
            }
        }
    }
}

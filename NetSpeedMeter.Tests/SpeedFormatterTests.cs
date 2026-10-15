using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Helpers;
using NetSpeedMeter.Models;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class SpeedFormatterTests
    {
        [TestMethod]
        public void Format_Zero_ReturnsZeroBytes()
        {
            string result = SpeedFormatter.Format(0, SpeedUnitMode.AutoBinary);
            Assert.AreEqual("0 B/s", result);
        }

        [TestMethod]
        public void Format_NegativeSpeed_ClampedToZero()
        {
            string result = SpeedFormatter.Format(-500, SpeedUnitMode.AutoBinary);
            Assert.AreEqual("0 B/s", result);
        }

        [TestMethod]
        public void Format_SmallBytes_ReturnsBytes()
        {
            string result = SpeedFormatter.Format(850, SpeedUnitMode.AutoBinary);
            Assert.AreEqual("850 B/s", result);
        }

        [TestMethod]
        public void Format_Kilobytes_ReturnsKB()
        {
            // 256 KB = 256 * 1024 = 262144 bytes
            string result = SpeedFormatter.Format(262144, SpeedUnitMode.AutoBinary);
            Assert.AreEqual("256 KB/s", result);
        }

        [TestMethod]
        public void Format_Megabytes_ReturnsMB()
        {
            // 2.45 MB = 2.45 * 1024 * 1024 = 2569011.2 bytes
            string result = SpeedFormatter.Format(2569011.2, SpeedUnitMode.AutoBinary);
            Assert.AreEqual("2.45 MB/s", result);
        }

        [TestMethod]
        public void Format_Gigabytes_ReturnsGB()
        {
            // 1.5 GB = 1.5 * 1024 * 1024 * 1024 = 1610612736 bytes
            string result = SpeedFormatter.Format(1610612736, SpeedUnitMode.AutoBinary);
            Assert.AreEqual("1.50 GB/s", result);
        }

        [TestMethod]
        public void Format_DecimalMode_Uses1000Base()
        {
            // 1500 bytes in decimal mode = 1.5 KB/s
            string result = SpeedFormatter.Format(1500, SpeedUnitMode.AutoDecimal);
            Assert.AreEqual("1.5 KB/s", result);
        }

        [TestMethod]
        public void Format_ForcedUnits_HonorsSelectedMode()
        {
            string kb = SpeedFormatter.Format(1048576, SpeedUnitMode.ForceKB);
            Assert.AreEqual("1024.0 KB/s", kb);

            string mb = SpeedFormatter.Format(1048576, SpeedUnitMode.ForceMB);
            Assert.AreEqual("1.00 MB/s", mb);
        }
    }
}

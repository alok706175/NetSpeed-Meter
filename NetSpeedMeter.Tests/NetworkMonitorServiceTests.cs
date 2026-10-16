using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Services;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class NetworkMonitorServiceTests
    {
        [TestMethod]
        public void TryCalculateDelta_NormalIncrease_ReturnsDifference()
        {
            long last = 100_000_000;
            long current = 105_000_000;
            double elapsed = 1.0;

            bool success = NetworkMonitorService.TryCalculateDelta(current, last, elapsed, out long delta);

            Assert.IsTrue(success);
            Assert.AreEqual(5_000_000, delta);
        }

        [TestMethod]
        public void TryCalculateDelta_32BitRollover_CalculatesWrappedDeltaCorrectly()
        {
            // Counter wrapped from near uint.MaxValue (4,294,967,295) past 0 to 10,000,000
            long last = 4_290_000_000L;
            long current = 10_000_000L;
            double elapsed = 1.0;

            bool success = NetworkMonitorService.TryCalculateDelta(current, last, elapsed, out long delta);

            Assert.IsTrue(success);
            // Expected wrapped delta = (uint.MaxValue - last) + current + 1
            long expected = ((long)uint.MaxValue - last) + current + 1;
            Assert.AreEqual(expected, delta);
            Assert.IsTrue(delta > 0);
        }

        [TestMethod]
        public void TryCalculateDelta_UnrealisticJumpOrReset_ReturnsFalse()
        {
            // If last was far above uint.MaxValue (64-bit counter) and dropped to 100,
            // this is an adapter reset/disconnect, not a 32-bit driver wrap.
            long last = 50_000_000_000L;
            long current = 100L;
            double elapsed = 1.0;

            bool success = NetworkMonitorService.TryCalculateDelta(current, last, elapsed, out long delta);

            Assert.IsFalse(success);
            Assert.AreEqual(0, delta);
        }
    }
}

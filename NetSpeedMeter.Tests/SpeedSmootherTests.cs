using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetSpeedMeter.Helpers;

namespace NetSpeedMeter.Tests
{
    [TestClass]
    public class SpeedSmootherTests
    {
        [TestMethod]
        public void Smooth_ZeroTraffic_ReturnsZero()
        {
            var smoother = new SpeedSmoother(0.65);
            double speed = smoother.Smooth(0);
            Assert.AreEqual(0.0, speed);
        }

        [TestMethod]
        public void Smooth_FirstSampleFromZero_RampsUpInstantlyWithoutLag()
        {
            var smoother = new SpeedSmoother(0.65);
            double initialSpeed = 10_000_000; // 10 MB/s
            double result = smoother.Smooth(initialSpeed);

            Assert.AreEqual(initialSpeed, result, 0.001);
        }

        [TestMethod]
        public void Smooth_SpikeFiltering_DampensSuddenBurst()
        {
            var smoother = new SpeedSmoother(0.65);
            smoother.Smooth(10_000_000); // 10 MB/s baseline

            // Sudden micro-spike to 20 MB/s
            double spikeResult = smoother.Smooth(20_000_000);

            // With alpha = 0.65:
            // Expected = 0.65 * 20_000_000 + 0.35 * 10_000_000 = 13_000_000 + 3_500_000 = 16_500_000
            double expected = 16_500_000;
            Assert.AreEqual(expected, spikeResult, 0.001);
            Assert.IsTrue(spikeResult < 20_000_000, "Spike should be dampened.");
            Assert.IsTrue(spikeResult > 10_000_000, "Spike should reflect higher throughput.");
        }

        [TestMethod]
        public void Smooth_TrafficStopped_DecaysAndSnapsToZeroPromptly()
        {
            var smoother = new SpeedSmoother(0.65);
            smoother.Smooth(10_000_000); // active transfer

            // 1st tick of 0 B/s: decays smoothly
            double tick1 = smoother.Smooth(0);
            Assert.IsTrue(tick1 > 0 && tick1 < 10_000_000);

            // 2nd tick of 0 B/s: snaps cleanly to 0.0
            double tick2 = smoother.Smooth(0);
            Assert.AreEqual(0.0, tick2, "Should snap to 0.0 by second consecutive zero tick.");
        }

        [TestMethod]
        public void Reset_ClearsSmootherState()
        {
            var smoother = new SpeedSmoother(0.65);
            smoother.Smooth(5_000_000);

            smoother.Reset();

            // Next sample should act as a brand new first sample
            double newSpeed = 2_000_000;
            double result = smoother.Smooth(newSpeed);
            Assert.AreEqual(newSpeed, result, 0.001);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_InvalidAlpha_ThrowsArgumentOutOfRangeException()
        {
            _ = new SpeedSmoother(0.0);
        }
    }
}

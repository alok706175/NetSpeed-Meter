using System;

namespace NetSpeedMeter.Helpers
{
    /// <summary>
    /// Exponential Moving Average (EMA) smoother for network speed readings.
    /// Dampens single-tick micro-spikes and jitter, while ramping up instantly on new traffic
    /// and decaying promptly to 0 when transfers complete.
    /// </summary>
    public class SpeedSmoother
    {
        private readonly double _alpha;
        private readonly bool _snapZeroImmediately;
        private double _smoothedSpeed = -1;
        private int _consecutiveZeros;

        /// <summary>
        /// Creates a new SpeedSmoother instance.
        /// </summary>
        /// <param name="alpha">
        /// Smoothing factor between 0.0 (exclusive) and 1.0 (inclusive).
        /// Default is 0.65, giving 65% weight to current sample and 35% to historical average.
        /// </param>
        /// <param name="snapZeroImmediately">
        /// If true, when raw speed drops to 0 or idle threshold, it immediately snaps to 0.0
        /// without lingering artificial decay cycles. Default is true.
        /// </param>
        public SpeedSmoother(double alpha = 0.65, bool snapZeroImmediately = true)
        {
            if (alpha <= 0.0 || alpha > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1.");
            }
            _alpha = alpha;
            _snapZeroImmediately = snapZeroImmediately;
        }

        /// <summary>
        /// Applies smoothing to a new raw speed sample (in bytes per second).
        /// </summary>
        public double Smooth(double rawSpeed)
        {
            // Negligible micro-traffic (< 32 B/s) or complete idle considered zero
            if (rawSpeed <= 0.0 || rawSpeed < 32.0)
            {
                _consecutiveZeros++;

                if (_snapZeroImmediately || _smoothedSpeed < 0 || _consecutiveZeros >= 2 || _smoothedSpeed < 256.0)
                {
                    _smoothedSpeed = 0.0;
                }
                else
                {
                    _smoothedSpeed *= (1.0 - _alpha);
                }

                return _smoothedSpeed;
            }

            _consecutiveZeros = 0;

            if (_smoothedSpeed <= 0.0)
            {
                // Instant ramp up when activity starts from idle
                _smoothedSpeed = rawSpeed;
            }
            else
            {
                // Exponential moving average filter for active traffic
                _smoothedSpeed = (_alpha * rawSpeed) + ((1.0 - _alpha) * _smoothedSpeed);
            }

            return _smoothedSpeed;
        }

        /// <summary>
        /// Resets the smoother state (e.g. on adapter switch or monitoring restart).
        /// </summary>
        public void Reset()
        {
            _smoothedSpeed = -1;
            _consecutiveZeros = 0;
        }
    }
}

using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Counts a value.
    /// </summary>
    public class ValueCounter : Session
    {
        private readonly object SyncLock = new object();

        /// <summary>
        /// The value to start with.
        /// </summary>
        public long InitialValue { get; private set; }

        /// <summary>
        /// The current value.
        /// </summary>
        public long Value { get; private set; }

        /// <summary>
        /// The maximum value used for calculating progress.
        /// </summary>
        public long Max { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="initialValue">The value to start with.</param>
        public ValueCounter(long initialValue = 0)
        {
            InitialValue = initialValue;
        }

        /// <summary>
        /// Sets the value to the initial value.
        /// </summary>
        protected override void OnStarting()
        {
            base.OnStarting();

            Value = InitialValue;
        }

        /// <summary>
        /// Adds 1 to the value.
        /// </summary>
        /// <returns>The value after the increment.</returns>
        public long Increment()
        {
            return Add(1);
        }

        /// <summary>
        /// Substracts 1 of the value.
        /// </summary>
        /// <returns>The value after the decrement.</returns>
        public long Decrement()
        {
            return Add(-1);
        }

        /// <summary>
        /// Adds (or substracts) the given value to value.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The value after the add.</returns>
        public long Add(long value)
        {
            lock (SyncLock)
            {
                Value += value;

                return Value;
            }
        }

        /// <summary>
        /// Calculates statistics about this counter.
        /// </summary>
        /// <param name="reset">When true, resets this counter after the statistics are calculated.</param>
        /// <param name="value">The current value.</param>
        /// <param name="duration">The duration since this counter started.</param>
        /// <param name="valuesPerSecond">The value per second since this counter started.</param>
        /// <param name="percentage">The percentage of Value/Max.</param>
        public void GetStatistics(bool reset, out long value, out TimeSpan duration, out double valuesPerSecond, out double percentage)
        {
            lock (SyncLock)
            {
                value = Value;
                duration = Elapsed;
                valuesPerSecond = duration != default ? value / duration.TotalSeconds : 0;
                percentage = Max > 0 ? (double)Value / (double)Max : 0;

                if (reset)
                    Restart();
            }
        }
    }
}

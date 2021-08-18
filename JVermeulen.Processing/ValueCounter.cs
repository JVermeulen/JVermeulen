using System;

namespace JVermeulen.Processing
{
    public class ValueCounter : Session
    {
        private readonly object SyncLock = new object();
        
        public long InitialValue { get; private set; }
        public long Value { get; private set; }
        public long Max { get; set; }

        public ValueCounter(long initialValue = 0)
        {
            InitialValue = initialValue;
        }

        public override void OnStarting()
        {
            Value = InitialValue;
        }

        public long Increment()
        {
            return Add(1);
        }

        public long Decrement()
        {
            return Add(-1);
        }

        public long Add(long value)
        {
            lock (SyncLock)
            {
                Value += value;

                return Value;
            }
        }

        public long GetStatistics(bool reset, out long value, out TimeSpan duration, out double valuesPerSecond, out double percentage)
        {
            lock (SyncLock)
            {
                value = Value;
                duration = Duration;
                valuesPerSecond = duration != default ? value / duration.TotalSeconds : 0;
                percentage = Max > 0 ? (double)Value / (double)Max : 0;

                if (reset)
                    Restart();

                return value;
            }
        }
    }
}

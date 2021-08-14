using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class ValueCounter
    {
        private readonly object SyncLock = new object();

        private TimeCounter Timer { get; set; }

        public double InitialValue { get; private set; }
        public double Value { get; private set; }
        public double Max { get; set; }

        private ValueCounter SubCounter { get; set; }

        public ValueCounter(double initialValue, bool createSubCounter = false)
        {
            InitialValue = initialValue;
            Value = InitialValue;

            if (createSubCounter)
                SubCounter = new ValueCounter(0, false);
        }

        public double Increment()
        {
            return Add(1);
        }

        public double Decrement()
        {
            return Add(-1);
        }

        public double Add(double value)
        {
            lock (SyncLock)
            {
                Value += value;

                SubCounter?.Add(value);

                return Value;
            }
        }

        public double Reset()
        {
            lock (SyncLock)
            {
                Value = InitialValue;

                SubCounter?.Reset();

                Timer.Restart();

                return Value;
            }
        }

        public double GetValue(bool reset, out double valuePerSecond, out double percentage)
        {
            valuePerSecond = 0;
            percentage = 0;

            lock (SyncLock)
            {
                var value = Value;

                valuePerSecond = Timer.Duration != default ? value / Timer.Duration.TotalSeconds : 0;
                percentage = Max > 0 ? Value / Max : 0;

                if (reset)
                    Reset();

                return value;
            }
        }

        public double GetSubValue(out double valuePerSecond)
        {
            valuePerSecond = 0;

            return SubCounter?.GetValue(true, out valuePerSecond, out _) ?? 0;
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}

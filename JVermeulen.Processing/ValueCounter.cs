using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class ValueCounter : TimeCounter
    {
        private readonly object SyncLock = new object();

        public double Value { get; private set; }
        public double Max { get; set; }

        private ValueCounter SubCounter { get; set; }

        public ValueCounter(bool autoStart, bool createSubCounter = false) : base(autoStart)
        {
            if (createSubCounter)
                SubCounter = new ValueCounter(autoStart, false);
        }

        public void Add(double value)
        {
            if (IsStarted)
            {
                lock (SyncLock)
                {
                    Value += value;

                    SubCounter?.Add(value);
                }
            }
        }

        public void Reset()
        {
            lock (SyncLock)
            {
                Value = 0;

                SubCounter?.Reset();

                base.Restart();
            }
        }

        public double GetValue(bool reset, out double valuePerSecond, out double percentage)
        {
            valuePerSecond = 0;
            percentage = 0;

            lock (SyncLock)
            {
                var value = Value;

                valuePerSecond = Duration != default ? value / Duration.TotalSeconds : 0;
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

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}

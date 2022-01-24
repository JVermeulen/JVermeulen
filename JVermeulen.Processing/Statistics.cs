using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class Statistics<TSubject, TAction> where TSubject : Enum where TAction : Enum
    {
        public string Name { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime StoppedAt { get; set; }

        public Dictionary<(TSubject, TAction), long> Values { get; set; }
        private readonly object ValuesLock = new object();

        public Statistics(string name)
        {
            Name = name;

            StartedAt = DateTime.Now;
            Values = new Dictionary<(TSubject, TAction), long>();
        }

        public void Add(TSubject subject, TAction action, int value = 1)
        {
            lock (ValuesLock)
            {
                (TSubject, TAction) key = (subject, action);

                if (!Values.ContainsKey(key))
                    Values.Add(key, 0);

                Values[key] += value;
            }
        }

        public StatisticsFrame<TSubject, TAction> Next()
        {
            lock (ValuesLock)
            {
                StoppedAt = DateTime.Now;

                var frame = new StatisticsFrame<TSubject, TAction>(Name, StartedAt, StoppedAt, Values);

                StartedAt = StoppedAt;
                StoppedAt = default;
                Values = new Dictionary<(TSubject, TAction), long>();

                return frame;
            }
        }
    }
}

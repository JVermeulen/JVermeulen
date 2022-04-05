using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Monitoring
{
    public record StatisticsFrame<TSubject, TAction> where TSubject : Enum where TAction : Enum
    {
        public string Name { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime StoppedAt { get; init; }
        public ReadOnlyDictionary<(TSubject, TAction), long> Values { get; init; }

        public StatisticsFrame(string name, DateTime startedAt, DateTime stoppedAt, Dictionary<(TSubject, TAction), long> values)
        {
            Name = name;
            StartedAt = startedAt;
            StoppedAt = stoppedAt;
            Values = new ReadOnlyDictionary<(TSubject, TAction), long>(values);
        }

        public long? GetValue(TSubject subject, TAction action)
        {
            var key = (subject, action);

            if (Values.ContainsKey(key))
                return Values[key];
            else
                return null;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{Name} Statistics:");

            var values = Values.OrderBy(v => v.Key);

            foreach (var value in values)
            {
                var duration = StoppedAt - StartedAt;
                var average = duration != default ? value.Value / duration.TotalSeconds : 0;

                builder.AppendLine($"- Number of {value.Key.Item1} {value.Key.Item2}: {value.Value} ({average:N1} /s)");
            }

            return builder.ToString();
        }
    }
}

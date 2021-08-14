using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class Progress : IDisposable
    {
        public string Message { get; private set; }
        public int Current { get; private set; }
        public int Total { get; private set; }
        public double Percentage => Total > 0 ? ((double)Current / (double)Total) : 0;

        public DateTime BeginTimestamp { get; private set; }
        public DateTime EndTimestamp { get; private set; }

        public Progress(string message, int total)
        {
            Message = message;
            Current = 0;
            Total = total;

            BeginTimestamp = DateTime.Now;
        }

        public void Increment()
        {
            Current++;

            EndTimestamp = DateTime.Now;
        }

        public override string ToString()
        {
            var duration = EndTimestamp - BeginTimestamp;
            var fps = Current / duration.TotalSeconds;

            if (Current < Total)
                return $"{Percentage.ToString("P1")} ({fps.ToString("F1")} fps)";
            else
                return $"{Message}: {duration.ToString(@"hh\:mm\:ss")} ({fps.ToString("F2")} fps)";
        }

        public void Dispose()
        {
            EndTimestamp = DateTime.Now;
        }
    }
}

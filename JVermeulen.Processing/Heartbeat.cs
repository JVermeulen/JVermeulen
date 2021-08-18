using System;

namespace JVermeulen.Processing
{
    public class Heartbeat
    {
        public long Count { get; private set; }

        public Heartbeat(long count)
        {
            Count = count;
        }

        public override string ToString()
        {
            return $"Heartbeat ({Count})";
        }
    }
}

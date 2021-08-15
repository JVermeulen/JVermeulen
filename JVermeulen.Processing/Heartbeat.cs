using System;

namespace JVermeulen.Processing
{
    public class Heartbeat
    {
        public long Id { get; private set; }

        public Heartbeat(long id)
        {
            Id = id;
        }
    }
}

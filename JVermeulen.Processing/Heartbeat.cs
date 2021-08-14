using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class Heartbeat
    {
        public long Value { get; private set; }

        public Heartbeat(long value)
        {
            Value = value;
        }
    }
}

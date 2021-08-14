using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class Heartbeat
    {
        public string Name { get; private set; }
        public long Value { get; private set; }

        public Heartbeat(string name, long value)
        {
            Name = name;
            Value = value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public enum WsStatisticsAction
    {
        Stopped = 0,
        Started = 1,
        Stopping = 2,
        Starting = 3,
        Disconnected = 10,
        Connected = 11,
        Disconnecting = 12,
        Connecting = 13,
        Sent = 20,
        Received = 21,
    }
}

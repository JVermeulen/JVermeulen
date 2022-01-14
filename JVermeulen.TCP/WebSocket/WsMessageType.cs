using System;

namespace JVermeulen.TCP.WebSocket
{
    public enum WsFrameType
    {
        Handshake = -1,
        Continuation = 0,
        Text = 1,
        Binary = 2,
        Disconnect = 8,
        Ping = 9,
        Pong = 10,
    }
}

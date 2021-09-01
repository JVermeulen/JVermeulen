using System;
using System.Net.Sockets;

namespace JVermeulen.TCP.Core
{
    public class TcpException : Exception
    {
        public SocketError? SocketError { get; set; }

        public TcpException(string message, Exception innerException = null, SocketError? error = null) : base(message, innerException)
        {
            SocketError = error;
        }
    }
}

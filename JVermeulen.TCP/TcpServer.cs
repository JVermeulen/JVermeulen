using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    public class TcpServer<T> : BaseTcpClient<T>
    {
        private SocketAsyncEventArgs AcceptorEventArgs { get; set; }

        public TcpServer(ITcpEncoder<T> encoder, int port) : this(encoder, new IPEndPoint(IPAddress.Any, port))
        {
            //
        }

        public TcpServer(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint) : base(encoder, serverEndpoint, TimeSpan.FromSeconds(5))
        {
            //
        }

        public override void OnStarting()
        {
            base.OnStarting();

            AcceptorEventArgs = new SocketAsyncEventArgs();
            AcceptorEventArgs.Completed += OnClientConnecting;

            Socket = new Socket(ServerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(ServerEndPoint);
            ServerEndPoint = (IPEndPoint)Socket.LocalEndPoint;
            Socket.Listen();

            WaitForClientConnecting(AcceptorEventArgs);
        }

        private void WaitForClientConnecting(SocketAsyncEventArgs e)
        {
            if (Status == SessionStatus.Started || Status == SessionStatus.Starting)
            {
                e.AcceptSocket = null;

                if (!Socket.AcceptAsync(e))
                {
                    OnClientConnected(e);

                    WaitForClientConnecting(e);
                }
            }
        }

        private void OnClientConnecting(object sender, SocketAsyncEventArgs e)
        {
            OnClientConnected(e);

            WaitForClientConnecting(e);
        }

        public override string ToString()
        {
            return $"TCP Server ({ServerAddress})";
        }
    }
}

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
        public override bool IsServer => true;
        private SocketAsyncEventArgs AcceptorEventArgs { get; set; }

        public bool OptionBroadcastToAllClients { get; set; } = false;

        public TcpServer(ITcpEncoder<T> encoder, int port) : this(encoder, new IPEndPoint(IPAddress.Any, port))
        {
            //
        }

        public TcpServer(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint) : base(encoder, serverEndpoint, TimeSpan.FromSeconds(60))
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

            Accept(AcceptorEventArgs);
        }

        public override void OnStopping()
        {
            base.OnStopping();

            Socket?.Dispose();
        }

        private void Accept(SocketAsyncEventArgs e)
        {
            if (Status == SessionStatus.Started || Status == SessionStatus.Starting)
            {
                e.AcceptSocket = null;

                if (!Socket.AcceptAsync(e))
                    OnClientConnecting(this, e);
            }
        }

        private void OnClientConnecting(object sender, SocketAsyncEventArgs e)
        {
            OnClientConnected(e);

            if (e.SocketError == SocketError.Success)
            {
                Accept(e);
            }
        }

        protected override void OnSessionMessage(SessionMessage message)
        {
            base.OnSessionMessage(message);

            if (OptionBroadcastToAllClients && message.Value is TcpMessage<T> tcpMessage && tcpMessage.IsIncoming)
            {
                var sessions = Sessions.Where(s => s.IsConnected && s.RemoteAddress != tcpMessage.Sender).ToList();

                sessions.ForEach(s => s.Write(tcpMessage.Content));
            }
        }

        public override string ToString()
        {
            return $"TCP Server ({ServerAddress})";
        }
    }
}

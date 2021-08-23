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
    public class TcpServer<T> : TcpSocketBase<T>
    {
        public override bool IsServer => true;
        private SocketAsyncEventArgs AcceptorEventArgs { get; set; }

        public bool OptionBroadcastMessages { get; set; } = false;
        public bool OptionEchoMessages { get; set; }

        public TcpServer(ITcpEncoder<T> encoder, int port) : this(encoder, new IPEndPoint(IPAddress.Any, port))
        {
            //
        }

        public TcpServer(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint) : base(encoder, serverEndpoint, TimeSpan.FromSeconds(60))
        {
            //
        }

        protected override void OnStarting()
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

        protected override void OnStopping()
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

        protected override void OnTcpSessionReceive(SessionMessage message)
        {
            base.OnTcpSessionReceive(message);

            if (message.Value is TcpMessage<T> tcpMessage && tcpMessage.IsIncoming)
            {
                if (OptionBroadcastMessages)
                    Broadcast(tcpMessage.Content, tcpMessage.Sender);

                if (OptionEchoMessages)
                    Echo(tcpMessage.Content, tcpMessage.Sender);
            }
        }

        public void Echo(T content, string sender)
        {
            Send(content, s => s.IsConnected && s.RemoteAddress == sender);
        }

        public void Broadcast(T content, string sender)
        {
            Send(content, s => s.IsConnected && s.RemoteAddress != sender);
        }

        public override string ToString()
        {
            return $"TCP Server ({ServerAddress})";
        }
    }
}

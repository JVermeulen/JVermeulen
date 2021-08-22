using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    public class TcpClient<T> : TcpSocketBase<T>
    {
        public override bool IsServer => false;
        public bool IsConnected => Socket.Connected;

        protected IPEndPoint ClientEndPoint { get; set; }
        public string ClientAddress { get; private set; }

        private SocketAsyncEventArgs ConnectorEventArgs { get; set; }

        private readonly ManualResetEvent ConnectSignal = new ManualResetEvent(false);

        public bool OptionReconnectOnHeatbeat { get; set; } = true;

        public TcpClient(ITcpEncoder<T> encoder, string address, int port) : this(encoder, new IPEndPoint(IPAddress.Parse(address), port))
        {
            //
        }

        public TcpClient(ITcpEncoder<T> encoder, IPEndPoint serverEndPoint) : base(encoder, serverEndPoint, TimeSpan.FromSeconds(15))
        {
            //
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            ConnectorEventArgs = new SocketAsyncEventArgs();
            ConnectorEventArgs.RemoteEndPoint = ServerEndPoint;
            ConnectorEventArgs.Completed += OnClientConnecting;

            Connect();            
        }

        private void Connect()
        {
            try
            {
                if (Status == SessionStatus.Starting || Status == SessionStatus.Started)
                {
                    ConnectSignal.Reset();

                    Socket = new Socket(ServerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    
                    if (!Socket.ConnectAsync(ConnectorEventArgs))
                        OnClientConnecting(this, ConnectorEventArgs);

                    ConnectSignal.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Outbox.Add(new SessionMessage(this, ex));
            }
        }

        private void OnClientConnecting(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ClientEndPoint = (IPEndPoint)e.ConnectSocket.LocalEndPoint;
                ClientAddress = ClientEndPoint?.ToString();
            }
            else
            {
                ClientEndPoint = null;
                ClientAddress = null;
            }

            ConnectSignal.Set();

            OnClientConnected(e);
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (!IsConnected && OptionReconnectOnHeatbeat)
                Connect();                
        }

        public override string ToString()
        {
            if (ClientAddress == null)
                return $"TCP Client";
            else
                return $"TCP Client ({ClientAddress})";
        }
    }
}

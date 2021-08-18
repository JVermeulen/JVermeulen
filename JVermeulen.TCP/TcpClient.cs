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
    public class TcpClient<T> : BaseTcpClient<T>
    {
        private IPEndPoint ClientEndPoint { get; set; }
        public string ClientAddress { get; private set; }

        private SocketAsyncEventArgs ConnectorEventArgs { get; set; }

        private readonly ManualResetEvent ConnectSignal = new ManualResetEvent(false);

        public TcpClient(ITcpEncoder<T> encoder, string address, int port) : this(encoder, new IPEndPoint(IPAddress.Parse(address), port))
        {
            //
        }

        public TcpClient(ITcpEncoder<T> encoder, IPEndPoint serverEndPoint) : base(encoder, serverEndPoint, TimeSpan.FromSeconds(5))
        {
            //
        }

        public override void OnStarting()
        {
            base.OnStarting();

            ConnectorEventArgs = new SocketAsyncEventArgs();
            ConnectorEventArgs.RemoteEndPoint = ServerEndPoint;
            ConnectorEventArgs.Completed += OnClientConnecting;

            Socket = new Socket(ServerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                if (!Socket.ConnectAsync(ConnectorEventArgs))
                {
                    OnClientConnected(ConnectorEventArgs);
                }
            }
            catch (Exception ex)
            {
                //
            }
        }

        private void OnClientConnecting(object sender, SocketAsyncEventArgs e)
        {
            OnClientConnected(e);
        }

        public override void OnStopping()
        {
            base.OnStopping();
        }

        public override string ToString()
        {
            return $"TCP Client {ClientAddress}";
        }
    }
}

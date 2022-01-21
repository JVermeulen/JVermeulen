using JVermeulen.App;
using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsClient : Actor
    {
        public TcpConnector Connector { get; private set; }
        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public WsSession Session { get; private set; }
        public bool IsConnected => Session != null && Session.IsConnected;

        public string Hostname { get; set; }
        protected IPEndPoint ServerEndPoint { get; set; }
        public string ServerAddress { get; private set; }
        protected IPEndPoint ClientEndPoint { get; set; }
        public string ClientAddress { get; private set; }

        public TimeSpan OptionConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool OptionReconnectOnHeatbeat { get; set; } = true;

        public WsClient(ITcpEncoder<WsContent> encoder, string address, int port)
        {
            if (NetworkInfo.TryGetDnsInfo(address, AddressFamily.InterNetwork, out string hostname, out IPAddress[] ipAddresses))
            {
                ServerEndPoint = new IPEndPoint(ipAddresses[0], port);

                Encoder = encoder;
                ServerAddress = $"ws://{ServerEndPoint}";

                Connector = new TcpConnector(ServerEndPoint);
                Connector.ClientConnected += OnClientConnected;
                Connector.ClientDisconnected += OnClientDisconnected;
            }
            else
            {
                //
            }
        }

        public WsClient(ITcpEncoder<WsContent> encoder, IPEndPoint serverEndpoint) : base(TimeSpan.FromSeconds(15))
        {
            Encoder = encoder;
            ServerEndPoint = serverEndpoint;
            ServerAddress = $"ws://{ServerEndPoint}";

            Connector = new TcpConnector(serverEndpoint);
            Connector.ClientConnected += OnClientConnected;
            Connector.ClientDisconnected += OnClientDisconnected;
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            Connector.Start(false);
        }

        protected override void OnStopping()
        {
            base.OnStarting();

            Connector.Stop();
        }

        private void OnClientConnected(object sender, TcpConnection e)
        {
            Session = new WsSession(e, Encoder);
            Session.MessageBox.SubscribeSafe(OnMessageReceived);
            Session.Start();
            Session.Handshake();
        }

        private void OnClientDisconnected(object sender, TcpConnection e)
        {
            Session?.Dispose();
            Session = null;
        }

        private void OnMessageReceived(ContentMessage<WsContent> message)
        {
            if (message.IsIncoming)
            {
                Console.WriteLine($"Message received: {message.Content}");
            }
            else
            {
                Console.WriteLine($"Message sent: {message.Content}");
            }
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (!IsConnected && Connector.IsStarted && OptionReconnectOnHeatbeat)
                Connector.Start(false);
        }

        public void Send(byte[] value)
        {
            if (IsConnected)
            {
                var content = new WsContent(value);

                Session.Send(content).ConfigureAwait(false);
            }
        }

        public void Send(string value)
        {
            if (IsConnected)
            {
                var content = new WsContent(value);

                Console.WriteLine($"Sending: {content}");

                Session.Send(content).ConfigureAwait(false);
            }
        }

        public override string ToString()
        {
            if (ClientAddress == null)
                return $"WS Client";
            else
                return $"WS Client ({ClientAddress})";
        }

        public override void Dispose()
        {
            base.Dispose();

            Connector.ClientConnected -= OnClientConnected;
            Connector.ClientDisconnected -= OnClientDisconnected;
        }
    }
}

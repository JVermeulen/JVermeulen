using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace JVermeulen.WebSockets
{
    public class WsServer : Actor
    {
        public TcpConnector Acceptor { get; private set; }

        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public string LocalAddress { get; private set; }
        public List<WsSession> Sessions { get; private set; }

        public bool OptionBroadcastMessages { get; set; } = false;
        public bool OptionEchoMessages { get; set; } = false;

        public WsServer(ITcpEncoder<WsContent> encoder, int port) : this(encoder, new IPEndPoint(IPAddress.Any, port))
        {
            //
        }

        public WsServer(ITcpEncoder<WsContent> encoder, IPEndPoint serverEndpoint) : base(TimeSpan.FromSeconds(5))
        {
            Encoder = encoder;

            Acceptor = new TcpConnector(serverEndpoint);
            Acceptor.ClientConnected += OnClientConnected;
            Acceptor.ClientDisconnected += OnClientDisconnected;

            LocalAddress = serverEndpoint.ToString();

            Sessions = new List<WsSession>();
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            Acceptor.Start(true);
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Acceptor.Stop();
        }

        protected virtual void OnClientConnected(object sender, TcpConnection e)
        {
            Console.WriteLine($"Client connected: {e}");

            var session = new WsSession(e, Encoder);
            session.MessageBox.SubscribeSafe(OnMessageReceived);
            session.Start();

            Sessions.Add(session);
        }

        protected virtual void OnClientDisconnected(object sender, TcpConnection e)
        {
            Console.WriteLine($"Client disconnected: {e}");

            var session = Sessions.Where(s => s.Connection == e).FirstOrDefault();

            if (session != null)
                Sessions.Remove(session);
        }

        private void OnMessageReceived(ContentMessage<WsContent> message)
        {
            if (message.IsIncoming)
            {
                Console.WriteLine($"Message received: {message.Content}");

                if (OptionBroadcastMessages)
                    Broadcast(message.Content, message.SenderAddress);

                if (OptionEchoMessages)
                    Echo(message.Content, message.SenderAddress);
            }
            else
            {
                Console.WriteLine($"Message sent: {message.Content}");
            }
        }

        public void Echo(WsContent content, string sender)
        {
            Send(content, s => s.IsConnected && s.RemoteAddress == sender);
        }

        public void Broadcast(WsContent content, string sender)
        {
            Send(content, s => s.IsConnected && (sender == null || s.RemoteAddress != sender));
        }

        public void Send(WsContent content, Func<WsSession, bool> query)
        {
            var sessions = Sessions.Where(s => s.Status == SessionStatus.Started).Where(query).ToList();

            sessions.ForEach(s => s.Send(content).ConfigureAwait(false));
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (Acceptor != null && !Acceptor.IsStarted)
                Acceptor.Start(true);
        }
    }
}

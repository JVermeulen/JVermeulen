using JVermeulen.Processing;
using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    public class TcpServer<T> : Actor
    {
        public TcpConnector Acceptor { get; set; }

        public bool OptionBroadcastMessages { get; set; } = false;
        public bool OptionEchoMessages { get; set; } = false;

        public ITcpEncoder<T> Encoder { get; private set; }
        public string LocalAddress { get; private set; }
        public List<TcpSession<T>> Sessions { get; private set; }

        public TcpServer(ITcpEncoder<T> encoder, int port) : this(encoder, new IPEndPoint(IPAddress.Any, port))
        {
            //
        }

        public TcpServer(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint) : base(TimeSpan.FromSeconds(5))
        {
            Encoder = encoder;

            Acceptor = new TcpConnector(serverEndpoint);
            Acceptor.ClientConnected += OnClientConnected;
            Acceptor.ClientDisconnected += OnClientDisconnected;
            
            LocalAddress = serverEndpoint.ToString();

            Sessions = new List<TcpSession<T>>();
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
            var session = new TcpSession<T>(e, Encoder);
            session.MessageBox.SubscribeSafe(OnTcpMessage);
            session.Start();

            Sessions.Add(session);
        }

        protected virtual void OnTcpMessage(ContentMessage<T> message)
        {
            //
        }

        protected virtual void OnClientDisconnected(object sender, TcpConnection e)
        {
            var session = Sessions.Where(s => s.Connection == e).FirstOrDefault();

            if (session != null)
                Sessions.Remove(session);
        }

        //protected override void OnTcpSessionStatus(SessionMessage message)
        //{
        //    if (message.Content is ContentMessage<T> tcpMessage && tcpMessage.IsIncoming)
        //    {
        //        if (OptionBroadcastMessages)
        //            Broadcast((T)tcpMessage.Content, tcpMessage.SenderAddress);

        //        if (OptionEchoMessages)
        //            Echo((T)tcpMessage.Content, tcpMessage.SenderAddress);
        //    }
        //}

        protected override void OnReceive(SessionMessage message)
        {
            base.OnReceive(message);

            var content = message.Find(m => m.ContentIsTypeof<string>());

            if (content != null)
                Broadcast((T)content.Content, null);
        }

        public void Echo(T content, string sender)
        {
            Send(content, s => s.IsConnected && s.RemoteAddress == sender);
        }

        public void Broadcast(T content, string sender)
        {
            Send(content, s => s.IsConnected && (sender == null || s.RemoteAddress != sender));
        }

        public void Send(T content, Func<TcpSession<T>, bool> query)
        {
            var sessions = Sessions.Where(s => s.Status == SessionStatus.Started).Where(query).ToList();

            sessions.ForEach(s => s.Send(content));
        }

        public override string ToString()
        {
            return $"TCP Server ({LocalAddress})";
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            Console.WriteLine($"Connected clients: {Sessions.Where(S => S.IsConnected).Count()}");
        }

        public override void Dispose()
        {
            base.Dispose();

            Acceptor.ClientConnected -= OnClientConnected;
            Acceptor.ClientDisconnected -= OnClientDisconnected;
        }
    }
}

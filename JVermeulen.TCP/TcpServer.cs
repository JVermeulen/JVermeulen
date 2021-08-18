using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    public class TcpServer<T> : HeartbeatSession
    {
        private Socket Socket { get; set; }
        private SocketAsyncEventArgs AcceptorEventArgs { get; set; }
        public List<TcpSession<T>> Sessions { get; private set; }
        private IPEndPoint LocalEndPoint { get; set; }
        public string ServerAddress { get; private set; }
        public ITcpEncoder<T> Encoder { get; private set; }
        public List<TcpSession<T>> ConnectedSessions => Sessions.Where(s => s.Socket.Connected).ToList();

        public long NumberOfBytesReceived => (long)Sessions.Sum(s => s.NumberOfBytesReceived.Value);
        public long NumberOfBytesSent => (long)Sessions.Sum(s => s.NumberOfBytesSent.Value);
        public long NumberOfMessagesReceived => (long)Sessions.Sum(s => s.NumberOfMessagesReceived.Value);
        public long NumberOfMessagesSent => (long)Sessions.Sum(s => s.NumberOfMessagesSent.Value);
        public long NumberOfConnectedClients => ConnectedSessions.Count;

        public SubscriptionQueue<SessionMessage> MessageQueue { get; private set; }

        public TcpServer(ITcpEncoder<T> encoder, int port) : this(encoder, new IPEndPoint(IPAddress.Any, port))
        {
            //
        }

        public TcpServer(ITcpEncoder<T> encoder, IPEndPoint localEndpoint) : base(TimeSpan.FromSeconds(5))
        {
            LocalEndPoint = localEndpoint;
            ServerAddress = LocalEndPoint.ToString();
            Encoder = encoder;

            Sessions = new List<TcpSession<T>>();

            MessageQueue = new SubscriptionQueue<SessionMessage>();
        }

        public virtual void OnErrorOccured(SocketError error)
        {
            Console.WriteLine(error.ToString());
        }

        public override void OnStarting()
        {
            base.OnStarting();

            AcceptorEventArgs = new SocketAsyncEventArgs();
            AcceptorEventArgs.Completed += OnClientConnecting;
            Socket = new Socket(LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(LocalEndPoint);
            LocalEndPoint = (IPEndPoint)Socket.LocalEndPoint;
            Socket.Listen();

            WaitForClients(AcceptorEventArgs);
        }

        private void WaitForClients(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;

            if (!Socket.AcceptAsync(e))
                OnClientConnected(e);
        }

        private void OnClientConnecting(object sender, SocketAsyncEventArgs e)
        {
            if (Status == SessionStatus.Started)
                OnClientConnected(e);
        }

        private void OnClientConnected(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var session = new TcpSession<T>(e.AcceptSocket, true, Encoder);
                session.MessageQueue.Subscribe(e => MessageQueue.Enqueue(new SessionMessage(this, e)));
                session.Subscribe(e => Queue.Enqueue(new SessionMessage(this, e)));
                session.Start();

                Sessions.Add(session);
            }
            else
            {
                OnErrorOccured(e.SocketError);
            }

            if (Status == SessionStatus.Started)
                WaitForClients(e);
        }

        public override void OnStopping()
        {
            base.OnStopping();

            Sessions.ForEach(s => s.Stop());

            while (MessageQueue.NumberOfValuesPending > 0)
                Task.Delay(10).Wait();
        }

        public void Send(T content, string clientAddress = "*")
        {
            var sessions = clientAddress == "*" ? ConnectedSessions : ConnectedSessions.Where(c => clientAddress != null && clientAddress.Equals(c.RemoteAddress, StringComparison.OrdinalIgnoreCase)).ToList();

            sessions.ForEach(s => s.Write(content));
        }

        public override void OnHeartbeatReceived(long count)
        {
            var yesterday = DateTime.Now.AddDays(-1);

            var oldSessions = Sessions.Where(s => s.Status == SessionStatus.Stopped && s.StoppedAt < yesterday);

            foreach (var session in oldSessions)
            {
                session.Dispose();

                Sessions.Remove(session);
            }
        }

        public override string ToString()
        {
            return $"TCP Server ({ServerAddress})";
        }
    }
}

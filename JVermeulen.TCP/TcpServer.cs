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
    public class TcpServer<T> : HeartbeatSession
    {
        private Socket Socket { get; set; }
        private SocketAsyncEventArgs AcceptorEventArgs { get; set; }
        private IPEndPoint LocalEndPoint { get; set; }
        public string ServerAddress { get; private set; }

        public ITcpEncoder<T> Encoder { get; private set; }
        public List<TcpSession<T>> Sessions { get; private set; }
        public List<TcpSession<T>> ConnectedSessions => Sessions.Where(s => s.Socket.Connected).ToList();

        public SubscriptionQueue<SessionMessage> MessageQueue { get; private set; }
        public TcpReport Statistics { get; private set; }

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
            Statistics = new TcpReport();
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
                session.Subscribe(OnMessageQueue);
                session.MessageQueue.Subscribe(OnMessageQueue);
                session.Start();

                Sessions.Add(session);
            }
            else
            {
                Queue.Enqueue(new SessionMessage(this, e.SocketError));
            }

            if (Status == SessionStatus.Started)
                WaitForClients(e);
        }

        private void OnMessageQueue(SessionMessage message)
        {
            MessageQueue.Enqueue(new SessionMessage(this, message));

            if (message.Value is SessionStatus sessionStatus)
            {
                if (sessionStatus == SessionStatus.Started)
                    Statistics.NumberOfConnectedClients++;
                else if (sessionStatus == SessionStatus.Stopped)
                    Statistics.NumberOfDisconnectedClients++;
            }
            else if (message.Value is TcpMessage<T> tcpMessage)
            {
                if (tcpMessage.IsIncoming)
                {
                    Statistics.NumberOfBytesReceived += tcpMessage.ContentInBytes;
                    Statistics.NumberOfMessagesReceived++;
                }
                else
                {
                    Statistics.NumberOfBytesSent += tcpMessage.ContentInBytes;
                    Statistics.NumberOfMessagesSent++;
                }
            }
        }

        public override void OnStopping()
        {
            base.OnStopping();

            Sessions.ForEach(s => s.Stop());

            // Wait for empty MessageQueue
            while (MessageQueue.NumberOfValuesPending > 0)
                Task.Delay(10).Wait();
        }

        public void Send(T content, string filterClientAddress = null)
        {
            var sessions = filterClientAddress == null ? ConnectedSessions : ConnectedSessions.Where(c => filterClientAddress.StartsWith(filterClientAddress, StringComparison.OrdinalIgnoreCase)).ToList();

            sessions.ForEach(s => s.Write(content));
        }

        public override void OnHeartbeatReceived(long count)
        {
            var yesterday = DateTime.Now.AddDays(-1);

            CleanupSessions(yesterday);
            CreateHeartbeatReport();
        }

        public void CleanupSessions(DateTime beforeStoppedAt)
        {
            var oldSessions = Sessions.Where(s => s.Status == SessionStatus.Stopped && s.StoppedAt < beforeStoppedAt);

            foreach (var session in oldSessions)
            {
                session.Dispose();

                Sessions.Remove(session);
            }
        }

        public TcpReport CreateSessionReport()
        {
            return new TcpReport
            {
                StartedAt = StartedAt,
                StoppedAt = StoppedAt != default ? StoppedAt : DateTime.Now,
                NumberOfConnectedClients = Sessions.Count(),
                NumberOfDisconnectedClients = Sessions.Count(s => !s.IsConnected),
                NumberOfBytesReceived = Sessions.Sum(s => s.NumberOfBytesReceived.Value),
                NumberOfBytesSent = Sessions.Sum(s => s.NumberOfBytesSent.Value),
                NumberOfMessagesReceived = Sessions.Sum(s => s.NumberOfMessagesReceived.Value),
                NumberOfMessagesSent = Sessions.Sum(s => s.NumberOfMessagesSent.Value),
            };
        }

        private void CreateHeartbeatReport()
        {
            Statistics.StoppedAt = DateTime.Now;

            Queue.Enqueue(new SessionMessage(this, Statistics));

            Statistics = new TcpReport();
        }

        public override string ToString()
        {
            return $"TCP Server ({ServerAddress})";
        }
    }
}

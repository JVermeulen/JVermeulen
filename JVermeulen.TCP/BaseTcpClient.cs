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
    public abstract class BaseTcpClient<T> : HeartbeatSession
    {
        protected Socket Socket { get; set; }
        protected IPEndPoint ServerEndPoint { get; set; }
        public string ServerAddress { get; private set; }

        public ITcpEncoder<T> Encoder { get; private set; }
        public List<TcpSession<T>> Sessions { get; private set; }
        public List<TcpSession<T>> ConnectedSessions => Sessions.Where(s => s.Socket.Connected).ToList();

        public SubscriptionQueue<SessionMessage> MessageQueue { get; private set; }
        public TcpReport Statistics { get; private set; }

        public bool OptionSendReportOnHeartbeat { get; set; }
        public bool OptionCleanupSessionsOnHeartbeat { get; set; }

        public BaseTcpClient(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint, TimeSpan interval) : base(interval)
        {
            ServerEndPoint = serverEndpoint;
            ServerAddress = ServerEndPoint.ToString();
            Encoder = encoder;

            Sessions = new List<TcpSession<T>>();

            MessageQueue = new SubscriptionQueue<SessionMessage>();
            Statistics = new TcpReport();
        }

        public override void OnStopping()
        {
            base.OnStopping();

            Sessions.ForEach(s => s.Stop());

            // Wait for empty MessageQueue
            while (MessageQueue.NumberOfValuesPending > 0)
                Task.Delay(10).Wait();
        }

        protected void OnClientConnected(SocketAsyncEventArgs e)
        {
            if (Status == SessionStatus.Started || Status == SessionStatus.Starting)
            {
                if (e.SocketError == SocketError.Success)
                {
                    var session = new TcpSession<T>(e.AcceptSocket ?? e.ConnectSocket, true, Encoder);
                    session.Subscribe(OnSessionMessage);
                    session.MessageQueue.Subscribe(OnSessionMessage);
                    session.Start();

                    Sessions.Add(session);
                }
                else
                {
                    Queue.Enqueue(new SessionMessage(this, e.SocketError));
                }
            }
        }

        public void Send(T content, string filterRemoteAddress = null)
        {
            var sessions = filterRemoteAddress == null ? ConnectedSessions : ConnectedSessions.Where(c => c.RemoteAddress.StartsWith(filterRemoteAddress, StringComparison.OrdinalIgnoreCase)).ToList();

            sessions.ForEach(s => s.Write(content));
        }

        protected void OnSessionMessage(SessionMessage message)
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

        public override void OnHeartbeatReceived(long count)
        {
            var yesterday = DateTime.Now.AddDays(-1);

            if (OptionCleanupSessionsOnHeartbeat)
                CleanupSessions(yesterday);

            if (OptionSendReportOnHeartbeat)
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

        private void CreateHeartbeatReport()
        {
            Statistics.StoppedAt = DateTime.Now;

                Queue.Enqueue(new SessionMessage(this, Statistics));

            Statistics = new TcpReport();
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
    }
}

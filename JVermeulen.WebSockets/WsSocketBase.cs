using JVermeulen.Processing;
using JVermeulen.TCP;
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
    public abstract class WsSocketBase<T> : Actor
    {
        public abstract bool IsServer { get; }
        protected WebSocket Socket { get; set; }
        protected IPEndPoint ServerEndPoint { get; set; }
        public string ServerAddress { get; private set; }
        public ITcpEncoder<T> Encoder { get; private set; }
        public List<TcpSession<T>> Sessions { get; private set; }

        private TcpStatistics HeartbeatStatistics { get; set; }

        public bool OptionSendStatisticsOnHeartbeat { get; set; } = false;
        public bool OptionCleanupSessionsOnHeartbeat { get; set; } = false;

        public WsSocketBase(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint, TimeSpan interval) : base()
        {
            ServerEndPoint = serverEndpoint;
            ServerAddress = ServerEndPoint.ToString();
            Encoder = encoder;
            OptionHeartbeatInterval = interval;

            Sessions = new List<TcpSession<T>>();

            HeartbeatStatistics = new TcpStatistics();
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Sessions.ForEach(s => s.Stop());

            // Wait for empty MessageQueue
            while (Outbox.NumberOfMessagesPending > 0)
                Thread.Yield();
        }

        protected void OnClientConnected(SocketAsyncEventArgs e)
        {
            if (Status == SessionStatus.Starting || Status == SessionStatus.Started)
            {
                if (e.SocketError == SocketError.Success)
                {
                    var session = new TcpSession<T>(e, Encoder);
                    session.SubscribeSafe<TcpSession<T>, SessionStatus>(OnTcpSessionStatus);
                    //session.SubscribeSafe<ContentMessage<T>>(OnTcpMessage);
                    session.MessageBox.SubscribeSafe(OnTcpMessage);
                    session.MessageBox.OptionWriteToConsole = true;
                    session.Start();
                    Sessions.Add(session);
                }
                else
                {
                    Outbox.Add(new SessionMessage(this, e.SocketError));
                }
            }
        }

        public void Send(T content, TcpSession<T> session)
        {
            var message = new ContentMessage<T>(session.LocalAddress, session.RemoteAddress, false, true, content, null);

            Inbox.Add(new SessionMessage(this, message));
        }

        public void Send(T content)
        {
            var sessions = Sessions.Where(s => s.Status == SessionStatus.Started).ToList();

            sessions.ForEach(s => Send(content, s));
        }

        public void Send(T content, Func<TcpSession<T>, bool> where)
        {
            var sessions = Sessions.Where(s => s.Status == SessionStatus.Started).Where(where).ToList();

            sessions.ForEach(s => Send(content, s));
        }

        protected override void OnReceive(SessionMessage message)
        {
            base.OnReceive(message);

            if (message.ContentIsTypeof(out T content))
            {
                var contentMessage = new ContentMessage<T>(null, null, false, true, content);

                Send(contentMessage, s => s.IsConnected);
            }
            else if (message.Content is ContentMessage<T> tcpMessage && tcpMessage.IsRequest)
            {
                Send(tcpMessage, s => s.IsConnected && (tcpMessage.DestinationAddress == null || s.RemoteAddress == tcpMessage.DestinationAddress));
            }
        }

        private void Send(ContentMessage<T> message, Func<TcpSession<T>, bool> query)
        {
            var sessions = Sessions.Where(query);

            foreach (var session in sessions)
            {
                session.Send(message.Content);
            }
        }

        protected virtual void OnTcpSessionStatus(SessionMessage message)
        {
            Outbox.Add(new SessionMessage(this, message));

            if (message.Content is SessionStatus sessionStatus)
            {
                if (sessionStatus == SessionStatus.Started)
                    HeartbeatStatistics.NumberOfConnectedClients++;
                else if (sessionStatus == SessionStatus.Stopped)
                    HeartbeatStatistics.NumberOfDisconnectedClients++;
            }
        }

        private void OnTcpMessage(ContentMessage<T> message)
        {
            if (message.IsIncoming)
            {
                HeartbeatStatistics.NumberOfBytesReceived += message.ContentInBytes ?? 0;
                HeartbeatStatistics.NumberOfMessagesReceived++;
            }
            else
            {
                HeartbeatStatistics.NumberOfBytesSent += message.ContentInBytes ?? 0;
                HeartbeatStatistics.NumberOfMessagesSent++;
            }
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            var yesterday = DateTime.Now.AddDays(-1);

            if (OptionCleanupSessionsOnHeartbeat)
                CleanupSessions(yesterday);

            if (OptionSendStatisticsOnHeartbeat)
                CreateHeartbeatStatistics();
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

        private void CreateHeartbeatStatistics()
        {
            HeartbeatStatistics.StoppedAt = DateTime.Now;

            Outbox.Add(new SessionMessage(this, HeartbeatStatistics));

            HeartbeatStatistics = new TcpStatistics();
        }

        public TcpStatistics CreateSessionStatistics()
        {
            return new TcpStatistics
            {
                StartedAt = StartedAt,
                StoppedAt = StoppedAt != default ? StoppedAt : DateTime.Now,
                NumberOfConnectedClients = Sessions.Count(),
                NumberOfDisconnectedClients = Sessions.Count(s => !s.IsConnected),
                NumberOfBytesReceived = Sessions.Sum(s => s.NumberOfBytesReceived),
                NumberOfBytesSent = Sessions.Sum(s => s.NumberOfBytesSent),
                NumberOfMessagesReceived = Sessions.Sum(s => s.NumberOfMessagesReceived),
                NumberOfMessagesSent = Sessions.Sum(s => s.NumberOfMessagesSent),
            };
        }
    }
}

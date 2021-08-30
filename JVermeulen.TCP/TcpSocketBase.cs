using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace JVermeulen.TCP
{
    public abstract class TcpSocketBase<T> : Actor
    {
        public abstract bool IsServer { get; }
        protected Socket Socket { get; set; }
        protected IPEndPoint ServerEndPoint { get; set; }
        public string ServerAddress { get; private set; }
        public ITcpEncoder<T> Encoder { get; private set; }
        public List<TcpSession<T>> Sessions { get; private set; }

        private TcpStatistics HeartbeatStatistics { get; set; }

        public bool OptionSendStatisticsOnHeartbeat { get; set; } = false;
        public bool OptionCleanupSessionsOnHeartbeat { get; set; } = false;

        public TcpSocketBase(ITcpEncoder<T> encoder, IPEndPoint serverEndpoint, TimeSpan interval) : base()
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
                    var session = new TcpSession<T>(e.AcceptSocket ?? e.ConnectSocket, IsServer, Encoder);
                    session.SubscribeSafe<TcpSession<T>, SessionStatus>(OnTcpSessionStatus);
                    session.SubscribeSafe<ContentMessage<T>>(OnTcpMessage);
                    session.MessageBox.SubscribeSafe(OnTcpSessionStatus);
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
            var message = new ContentMessage<T>(session.LocalAddress, session.RemoteAddress, false, false, content, null);

            Inbox.Add(new SessionMessage(this, message));
        }

        public void Send(T content, Func<TcpSession<T>, bool> where)
        {
            var sessions = Sessions.Where(where);

            foreach (var session in sessions)
            {
                Send(content, session);
            }
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
                var newMessage = (ContentMessage<T>)message.Clone();

                session.Send(newMessage);
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

        private void OnTcpMessage(SessionMessage message)
        {
            if (message.Content is ContentMessage<T> tcpMessage)
            {
                if (tcpMessage.IsIncoming)
                {
                    HeartbeatStatistics.NumberOfBytesReceived += tcpMessage.ContentInBytes ?? 0;
                    HeartbeatStatistics.NumberOfMessagesReceived++;
                }
                else
                {
                    HeartbeatStatistics.NumberOfBytesSent += tcpMessage.ContentInBytes ?? 0;
                    HeartbeatStatistics.NumberOfMessagesSent++;
                }
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

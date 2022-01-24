using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JVermeulen.WebSockets
{
    public class WsSessionManager
    {
        private List<WsSession> Sessions { get; set; }
        private readonly object SessionsLock = new object();

        public WsSessionManager()
        {
            Sessions = new List<WsSession>();
        }

        public void Stop()
        {
            lock (SessionsLock)
            {
                Sessions.ForEach(s => s.Stop());
            }
        }

        public void Cleanup(DateTime beforeStoppedAt)
        {
            lock (SessionsLock)
            {
                var oldSessions = Sessions.Where(s => s.Status == SessionStatus.Stopped && s.StoppedAt < beforeStoppedAt).ToList();

                foreach (var session in oldSessions)
                {
                    session.Dispose();

                    Sessions.Remove(session);
                }
            }
        }

        public void Add(WsSession session)
        {
            lock (SessionsLock)
            {
                Sessions.Add(session);
            }
        }

        public void Send(Content content, Func<WsSession, bool> query = null)
        {
            if (content.Value.Length > 0)
            {
                lock (SessionsLock)
                {
                    var activeSessions = Sessions.Where(s => s.Status == SessionStatus.Started);
                    var validsessions = query != null ? activeSessions.Where(query).ToList() : activeSessions.ToList();

                    validsessions.ForEach(s => s.Send(content).ConfigureAwait(false));
                }
            }
        }

        public int ConnectedCount()
        {
            lock (SessionsLock)
            {
                var connectedSessions = Sessions.Where(s => s.Status == SessionStatus.Started && s.IsConnected);

                return connectedSessions.Count();
            }
        }
    }
}

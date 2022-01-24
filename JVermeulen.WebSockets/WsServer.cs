using JVermeulen.Processing;
using JVermeulen.TCP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsServer : Actor
    {
        private HttpListener Listener { get; set; }
        public WsSessionManager Sessions { get; private set; }

        public ITcpEncoder<Content> Encoder { get; private set; }
        public bool IsSecure { get; private set; }
        public bool ContentIsText { get; private set; }
        public Uri ServerUri { get; private set; }

        public bool IsListening => Listener?.IsListening ?? false;
        public bool IsAccepting { get; private set; }

        public Statistics<WsStatisticsSubject, WsStatisticsAction> ServerStatistics { get; set; }

        public TimeSpan OptionKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
        public bool OptionBroadcastMessages { get; set; } = false;
        public bool OptionEchoMessages { get; set; } = false;
        public bool OptionLogToConsole { get; set; } = false;

        public WsServer(ITcpEncoder<Content> encoder, string serverUri, bool contentIsText) : base(TimeSpan.FromSeconds(15))
        {
            Encoder = encoder;
            SetServerUri(serverUri);
            ContentIsText = contentIsText;

            Sessions = new WsSessionManager();
            ServerStatistics = new Statistics<WsStatisticsSubject, WsStatisticsAction>($"Server ({ServerUri})");
        }

        private void SetServerUri(string serverUri)
        {
            var builder = new UriBuilder(serverUri);
            IsSecure = builder.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) || builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            builder.Scheme = IsSecure ? "https" : "http";
            ServerUri = builder.Uri;
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            if (StartListener())
                WaitForAcceptAsync().ConfigureAwait(false);

            if (OptionLogToConsole)
                Console.WriteLine($"[Server] Started: {ServerUri}");
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Sessions.Stop();

            Task.Delay(1000).Wait();

            Listener?.Stop();
        }

        protected override void OnStopped()
        {
            base.OnStopped();

            if (OptionLogToConsole)
                Console.WriteLine($"[Server] Stopped: {ServerUri}");
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (OptionLogToConsole)
                Console.WriteLine();

            if (Status == SessionStatus.Started)
            {
                if (!IsListening || !IsAccepting)
                {
                    if (StartListener())
                        WaitForAcceptAsync().ConfigureAwait(false);
                }
            }

            var statistics = ServerStatistics.Next();

            if (OptionLogToConsole && statistics.Values.Count > 0)
                Console.WriteLine(statistics.ToString());

            Outbox.Add(new SessionMessage(this, statistics));
        }

        private async Task WaitForAcceptAsync()
        {
            try
            {
                IsAccepting = true;

                while (Status == SessionStatus.Started && IsListening)
                {
                    var context = await Listener.GetContextAsync();

                    await OnAccept(context);
                }
            }
            catch (Exception ex)
            {
                OnExceptionOccured(this, ex);
            }
            finally
            {
                IsAccepting = false;
            }
        }

        private bool StartListener()
        {
            try
            {
                if (Listener != null && !Listener.IsListening)
                {
                    Listener.Stop();
                    Listener = null;
                }

                if (Listener == null)
                {
                    Listener = new HttpListener();
                    Listener.Prefixes.Add(ServerUri.ToString());
                    Listener.Start();
                }

                return true;
            }
            catch (Exception ex)
            {
                //Listener will be disposed on exception.
                Listener = null;

                OnExceptionOccured(this, ex);
            }

            return false;
        }

        protected override void OnExceptionOccured(object sender, Exception ex)
        {
            base.OnExceptionOccured(sender, ex);

            if (OptionLogToConsole)
                Console.WriteLine($"[Server] Exception: {GetExceptionMessageRecursive(ex)}");
        }

        private async Task OnAccept(HttpListenerContext context)
        {
            try
            {
                if (!context.Request.IsWebSocketRequest)
                    throw new ApplicationException("Request is not a WebSocket request.");

                var wsContext = await context.AcceptWebSocketAsync(null, OptionKeepAliveInterval);

                var session = new WsSession(Encoder, true, ContentIsText, ServerUri.ToString(), wsContext.WebSocket);
                session.Outbox.SubscribeSafe(OnSessionMessage);
                session.MessageBox.SubscribeSafe(OnContentMessage);

                Sessions.Add(session);

                session.Start();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                //TODO: replace with generic message
                context.Response.StatusDescription = ex.Message;
                context.Response.Close();
            }
        }

        private void OnSessionMessage(SessionMessage message)
        {
            if (message.Find(out WsSession _, out Exception ex))
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Server] Exception: {GetExceptionMessageRecursive(ex)}");
            }
            else if (message.Find(out WsSession session, out SessionStatus status))
            {
                if (status == SessionStatus.Started)
                {
                    ServerStatistics.Add(WsStatisticsSubject.Clients, WsStatisticsAction.Connected);

                    if (OptionLogToConsole)
                        Console.WriteLine($"[Server] Connected: {session}");
                }
                else if (status == SessionStatus.Stopped)
                {
                    ServerStatistics.Add(WsStatisticsSubject.Clients, WsStatisticsAction.Disconnected);

                    if (OptionLogToConsole)
                        Console.WriteLine($"[Server] Disconnected: {session}");
                }
            }
        }

        private void OnContentMessage(ContentMessage<Content> message)
        {
            Console.ResetColor();

            if (message.IsIncoming)
            {
                ServerStatistics.Add(WsStatisticsSubject.Messages, WsStatisticsAction.Received);
                ServerStatistics.Add(WsStatisticsSubject.Bytes, WsStatisticsAction.Received, message.ContentInBytes ?? 0);

                //if (OptionLogToConsole)
                //    Console.WriteLine($"[Server] Received: {message.ContentInBytes} bytes");

                if (long.TryParse(message.SenderAddress, out long sessionId))
                {
                    if (OptionBroadcastMessages)
                        Broadcast(message.Content, sessionId);

                    if (OptionEchoMessages)
                        Echo(message.Content, sessionId);
                }
            }
            else
            {
                ServerStatistics.Add(WsStatisticsSubject.Messages, WsStatisticsAction.Sent);
                ServerStatistics.Add(WsStatisticsSubject.Bytes, WsStatisticsAction.Sent, message.ContentInBytes ?? 0);

                //if (OptionLogToConsole)
                //    Console.WriteLine($"[Server] Sent: {message.ContentInBytes} bytes");
            }
        }

        public void Echo(Content content, long sender)
        {
            Sessions.Send(content, s => s.IsConnected && s.SessionId == sender);
        }

        public void Broadcast(Content content, long sender)
        {
            Sessions.Send(content, s => s.IsConnected && s.SessionId != sender);
        }

        public void Send(byte[] value)
        {
            if (Status == SessionStatus.Started)
            {
                var content = new Content(value);

                Sessions.Send(content);
            }
        }

        public void Send(string value)
        {
            if (Status == SessionStatus.Started)
            {
                var text = Encoding.UTF8.GetBytes(value);
                var content = new Content(text);

                Sessions.Send(content);
            }
        }

        public override void Dispose()
        {
            Stop();

            base.Dispose();
        }
    }
}

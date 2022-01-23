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

        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public bool IsSecure { get; private set; }
        public Uri ServerUri { get; private set; }

        public bool IsListening => Listener?.IsListening ?? false;
        public bool IsAccepting { get; private set; }

        public TimeSpan OptionKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
        public bool OptionBroadcastMessages { get; set; } = false;
        public bool OptionEchoMessages { get; set; } = false;
        public bool OptionLogToConsole { get; set; } = false;

        public WsServer(ITcpEncoder<WsContent> encoder, string serverUri) : base(TimeSpan.FromSeconds(15))
        {
            Encoder = encoder;

            SetServerUri(serverUri);

            Sessions = new WsSessionManager();
        }

        private void SetServerUri(string serverUri)
        {
            var builder = new UriBuilder(serverUri);
            IsSecure = builder.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) || builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            builder.Scheme = IsSecure ? "https" : "http";
            ServerUri = builder.Uri;
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            if (StartListener())
                WaitForAcceptAsync().ConfigureAwait(false);
        }

        protected override void OnStarted()
        {
            if (OptionLogToConsole)
                Console.WriteLine($"[Server] Started: {ServerUri}");

            base.OnStarted();
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
            if (Status == SessionStatus.Started)
            {
                if (!IsListening || !IsAccepting)
                {
                    if (StartListener())
                        WaitForAcceptAsync().ConfigureAwait(false);
                }
            }

            base.OnHeartbeat(heartbeat);
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

                var session = new WsSession(Encoder, true, ServerUri.ToString(), wsContext.WebSocket);
                session.Outbox.SubscribeSafe(OnSessionMessage);
                session.MessageBox.SubscribeSafe(OnMessageReceived);

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
                    if (OptionLogToConsole)
                        Console.WriteLine($"[Server] Connected: {session}");
                }
                else if (status == SessionStatus.Stopped)
                {
                    if (OptionLogToConsole)
                        Console.WriteLine($"[Server] Disconnected: {session}");
                }
            }
        }

        private void OnMessageReceived(ContentMessage<WsContent> message)
        {
            Console.ResetColor();

            if (message.IsIncoming)
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Server] Received: {message.ContentInBytes} bytes");

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
                if (OptionLogToConsole)
                    Console.WriteLine($"[Server] Sent: {message.ContentInBytes} bytes");
            }
        }

        public void Echo(WsContent content, long sender)
        {
            Sessions.Send(content, s => s.IsConnected && s.SessionId == sender);
        }

        public void Broadcast(WsContent content, long sender)
        {
            Sessions.Send(content, s => s.IsConnected && s.SessionId != sender);
        }

        public void Send(byte[] value)
        {
            if (Status == SessionStatus.Started)
            {
                var content = new WsContent(value);

                Sessions.Send(content);
            }
        }

        public void Send(string value)
        {
            if (Status == SessionStatus.Started)
            {
                var content = new WsContent(value);

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

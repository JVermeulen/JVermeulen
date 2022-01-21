using JVermeulen.Processing;
using JVermeulen.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsServer2 : Actor
    {
        private HttpListener Listener { get; set; }
        public List<WsSession2> Sessions { get; private set; }

        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public string ServerUrl => Listener.Prefixes.FirstOrDefault();

        public TimeSpan OptionKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
        public bool OptionBroadcastMessages { get; set; } = false;
        public bool OptionEchoMessages { get; set; } = false;

        public WsServer2(ITcpEncoder<WsContent> encoder, string url)
        {
            Encoder = encoder;

            Listener = new HttpListener();
            Listener.Prefixes.Add(url);

            Sessions = new List<WsSession2>();
        }

        protected override void OnStarting()
        {
            Listener.Start();
        }

        protected override void OnStarted()
        {
            Console.WriteLine($"[Server] Started: {ServerUrl}");

            WaitForAcceptAsync().ConfigureAwait(false);
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Listener.Stop();
        }

        protected override void OnStopped()
        {
            base.OnStopped();

            Console.WriteLine($"[Server] Stopped: {ServerUrl}");
        }

        private async Task WaitForAcceptAsync()
        {
            try
            {
                while (Status == SessionStatus.Started)
                {
                    var context = await Listener.GetContextAsync();

                    await OnAccept(context);
                }
            }
            catch (HttpListenerException httpListenerException)
            {
                if (httpListenerException.ErrorCode == 995)
                    Stop();
                else
                    throw;
            }
            catch
            {
                throw;
            }
        }

        private async Task OnAccept(HttpListenerContext context)
        {
            try
            {
                if (!context.Request.IsWebSocketRequest)
                    throw new ApplicationException("Request is not a WebSocket request.");

                var wsContext = await context.AcceptWebSocketAsync(null, OptionKeepAliveInterval);

                var session = new WsSession2(Encoder, true, ServerUrl, wsContext.WebSocket);
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
            if (message.Sender is WsSession2 session)
            {
                if (message.Content is SessionStatus status)
                {
                    if (status == SessionStatus.Started)
                        Console.WriteLine($"[Server] Connected: {session}");
                    else if (status == SessionStatus.Stopped)
                        Console.WriteLine($"[Server] Disconnected: {session}");
                }
            }
        }

        private void OnMessageReceived(ContentMessage<WsContent> message)
        {
            Console.ResetColor();

            if (message.IsIncoming)
            {
                Console.WriteLine($"[Server] Message received: {message.Content}");

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
                Console.WriteLine($"[Server] Message sent: {message.Content}");
            }
        }

        public void Echo(WsContent content, long sender)
        {
            Send(content, s => s.IsConnected && s.SessionId == sender);
        }

        public void Broadcast(WsContent content, long sender)
        {
            Send(content, s => s.IsConnected && s.SessionId != sender);
        }

        public void Send(WsContent content, Func<WsSession2, bool> query)
        {
            var sessions = Sessions.Where(s => s.Status == SessionStatus.Started).Where(query).ToList();

            sessions.ForEach(s => s.Send(content).ConfigureAwait(false));
        }
    }
}

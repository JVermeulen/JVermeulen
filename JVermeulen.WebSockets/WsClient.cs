using JVermeulen.App;
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
    public class WsClient : Actor
    {
        public ITcpEncoder<Content> Encoder { get; private set; }
        public bool IsSecure { get; private set; }
        public bool ContentIsText { get; private set; }
        public Uri ServerUri { get; private set; }
        public WsSessionManager Sessions { get; private set; }
        public bool IsConnected => Sessions.ConnectedCount() > 0;
        public bool IsConnecting { get; private set; }

        public TimeSpan OptionConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool OptionReconnectOnHeatbeat { get; set; } = true;
        public bool OptionLogToConsole { get; set; } = false;

        public WsClient(ITcpEncoder<Content> encoder, string serverUri, bool contentIsText) : base(TimeSpan.FromSeconds(15))
        {
            Encoder = encoder;
            SetServerUri(serverUri);
            ContentIsText = contentIsText;

            Sessions = new WsSessionManager();
        }

        private void SetServerUri(string serverUri)
        {
            var builder = new UriBuilder(serverUri);
            IsSecure = builder.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) || builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            builder.Scheme = IsSecure ? "wss" : "ws";
            ServerUri = builder.Uri;
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            WaitForConnect().ConfigureAwait(false);
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Sessions.Stop();
        }

        private async Task WaitForConnect()
        {
            var isConnected = await WaitForConnectAsync();
        }

        private async Task<bool> WaitForConnectAsync()
        {
            try
            {
                IsConnecting = true;

                var client = new ClientWebSocket();

                using (var timeout = new CancellationTokenSource(OptionConnectionTimeout))
                {
                    await client.ConnectAsync(ServerUri, timeout.Token);

                    if (client.State != WebSocketState.Open)
                        throw new ApplicationException($"Failed to connect ({client.State}).");

                    OnConnect(client);

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client] Warning: {GetExceptionMessageRecursive(ex)}");

                if (FindExceptionRecursive(ex, out SocketException socketException))
                    OnExceptionOccured(this, socketException);

                return false;
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private void OnConnect(ClientWebSocket context)
        {
            var session = new WsSession(Encoder, false, ContentIsText, ServerUri.ToString(), context);
            session.Outbox.SubscribeSafe(OnSessionMessage);
            session.MessageBox.SubscribeSafe(OnMessageReceived);

            Sessions.Add(session);

            session.Start();
        }

        private void OnSessionMessage(SessionMessage message)
        {
            if (message.Find(out WsSession _, out Exception ex))
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client] Exception: {GetExceptionMessageRecursive(ex)}");
            }
            else if (message.Find(out WsSession session, out SessionStatus status))
            {
                if (status == SessionStatus.Started)
                {
                    if (OptionLogToConsole)
                        Console.WriteLine($"[Client] Connected: {session}");
                }
                else if (status == SessionStatus.Stopped)
                {
                    if (OptionLogToConsole)
                        Console.WriteLine($"[Client] Disconnected: {session}");
                }
            }
        }

        private void OnMessageReceived(ContentMessage<Content> message)
        {
            Console.ResetColor();

            if (message.IsIncoming)
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client] Received: {message.ContentInBytes} bytes");
            }
            else
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client] Sent: {message.ContentInBytes} bytes");
            }
        }

        public void Send(byte[] value)
        {
            if (IsConnected)
            {
                var content = new Content(value);

                Sessions.Send(content);
            }
        }

        public void Send(string value)
        {
            if (IsConnected)
            {
                var data = Encoding.UTF8.GetBytes(value);
                var content = new Content(data);

                Sessions.Send(content);
            }
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (Status == SessionStatus.Started && !IsConnected && !IsConnecting)
                WaitForConnect().ConfigureAwait(false);
        }

        public bool Dns(out string message)
        {
            message = null;

            if (NetworkInfo.TryGetDnsInfo(ServerUri.Host, AddressFamily.InterNetwork, out string hostname, out IPAddress[] ipAddresses))
                message = $"DNS: {hostname} [{ipAddresses[0]}]";

            return message != null;
        }

        public bool Ping(out string message)
        {
            message = null;

            if (NetworkInfo.Ping(ServerUri.Host, out IPAddress ipAddress, out TimeSpan roundtrip))
                message = $"Ping: {ServerUri.Host} [{ipAddress}] {roundtrip.TotalMilliseconds:N0} ms";

            return message != null;
        }

        public override void Dispose()
        {
            Stop();

            base.Dispose();
        }
    }
}

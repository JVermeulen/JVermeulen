using JVermeulen.App;
using JVermeulen.Monitoring;
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
        public Statistics<WsStatisticsSubject, WsStatisticsAction> ClientStatistics { get; set; }

        public TimeSpan OptionConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool OptionReconnectOnHeatbeat { get; set; } = true;
        public bool OptionLogToConsole { get; set; } = false;
        public Tuple<string, string> OptionRequestHeader { get; set; } = null;

        public WsClient(ITcpEncoder<Content> encoder, string serverUri, bool contentIsText) : base(TimeSpan.FromSeconds(15))
        {
            Encoder = encoder;
            SetServerUri(serverUri);
            ContentIsText = contentIsText;

            Sessions = new WsSessionManager();
            ClientStatistics = new Statistics<WsStatisticsSubject, WsStatisticsAction>($"Client ({ServerUri})");
        }

        private void SetServerUri(string serverUri)
        {
            var builder = new UriBuilder(serverUri);
            IsSecure = builder.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) || builder.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            builder.Scheme = IsSecure ? "wss" : "ws";
            ServerUri = builder.Uri;
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            if (OptionLogToConsole)
            {
                if (Dns(out string dnsMessage))
                    Console.WriteLine($"[Client] {dnsMessage}");
                else
                    Console.WriteLine($"[Client] DNS: Failed");
            }
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

                if (OptionRequestHeader != null)
                    client.Options.SetRequestHeader(OptionRequestHeader.Item1, OptionRequestHeader.Item2);
                
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
            session.MessageBox.SubscribeSafe(OnContentMessage);

            Sessions.Add(session);

            session.Start();
        }

        private void OnSessionMessage(SessionMessage message)
        {
            if (message.Find(out WsSession session1, out Exception ex))
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client {session1.SessionId}] Exception: {GetExceptionMessageRecursive(ex)}");
            }
            else if (message.Find(out WsSession session2, out SessionStatus status))
            {
                if (status == SessionStatus.Started)
                {
                    ClientStatistics.Add(WsStatisticsSubject.Servers, WsStatisticsAction.Connected);

                    if (OptionLogToConsole)
                        Console.WriteLine($"[Client {session2.SessionId}] Connected: {session2}");
                }
                else if (status == SessionStatus.Stopped)
                {
                    ClientStatistics.Add(WsStatisticsSubject.Servers, WsStatisticsAction.Disconnected);

                    if (OptionLogToConsole)
                        Console.WriteLine($"[Client {session2.SessionId}] Disconnected: {session2}");
                }
            }
        }

        private void OnContentMessage(ContentMessage<Content> message)
        {
            if (message.IsIncoming)
            {
                if (OptionLogToConsole)
                {
                    ClientStatistics.Add(WsStatisticsSubject.Messages, WsStatisticsAction.Received);
                    ClientStatistics.Add(WsStatisticsSubject.Bytes, WsStatisticsAction.Received, message.ContentInBytes ?? 0);

                    //if (OptionLogToConsole)
                    //    Console.WriteLine($"[Client {message.SenderAddress}] Received: {message.ContentInBytes} bytes");
                }
            }
            else
            {
                ClientStatistics.Add(WsStatisticsSubject.Messages, WsStatisticsAction.Sent);
                ClientStatistics.Add(WsStatisticsSubject.Bytes, WsStatisticsAction.Sent, message.ContentInBytes ?? 0);

                //if (OptionLogToConsole)
                //    Console.WriteLine($"[Client {message.SenderAddress}] Sent: {message.ContentInBytes} bytes");
            }
        }

        public void Send(byte[] value)
        {
            if (IsConnected && value.Any())
            {
                var content = new Content(value);

                Sessions.Send(content);
            }
        }

        public void Send(string value, int repeat = 1, TimeSpan interval = default)
        {
            for (int i = 0; i < repeat; i++)
            {
                if (IsConnected && !string.IsNullOrEmpty(value))
                {
                    var data = Encoding.UTF8.GetBytes(value);
                    var content = new Content(data);

                    Sessions.Send(content);
                }

                if (i < repeat && interval != default)
                    Task.Delay(interval).Wait();
            }
        }

        //public static int TestDelay = 1000;

        //public void Test()
        //{
        //    var random = new Random();
        //    while (Status == SessionStatus.Started)
        //    {
        //        var data = Encoding.UTF8.GetBytes("12");
        //        var content = new Content(data);

        //        Sessions.Send(content);

        //        Task.Delay(TestDelay).Wait();
        //    }
        //}

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (Status == SessionStatus.Started && !IsConnected && !IsConnecting)
                WaitForConnect().ConfigureAwait(false);

            var statistics = ClientStatistics.Next();

            if (OptionLogToConsole && statistics.Values.Count > 0)
                Console.WriteLine(statistics.ToString());

            Outbox.Add(new SessionMessage(this, statistics));
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

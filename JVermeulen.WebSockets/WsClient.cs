using JVermeulen.Processing;
using JVermeulen.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsClient : Actor
    {
        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public string ServerUrl { get; private set; }

        public WsSession Session { get; private set; }
        public bool IsConnected => Session != null && Session.IsConnected;

        public TimeSpan OptionConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool OptionReconnectOnHeatbeat { get; set; } = true;
        public bool OptionLogToConsole { get; set; } = false;

        public WsClient(ITcpEncoder<WsContent> encoder, string url)
        {
            Encoder = encoder;
            ServerUrl = url;
        }

        protected override void OnStarted()
        {
            WaitForConnect();
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Session?.Stop();
        }

        private void WaitForConnect()
        {
            WaitForConnectAsync().ConfigureAwait(false);
        }

        private async Task WaitForConnectAsync()
        {
            var client = new ClientWebSocket();

            using (var timeout = new CancellationTokenSource(OptionConnectionTimeout))
            {
                await client.ConnectAsync(new Uri(ServerUrl), timeout.Token);

                if (client.State == WebSocketState.Open)
                    OnConnect(client);
            }
        }

        private void OnConnect(ClientWebSocket context)
        {
            if (Session == null)
            {
                Session = new WsSession(Encoder, false, ServerUrl, context);
                Session.Outbox.SubscribeSafe(OnSessionMessage);
                Session.MessageBox.SubscribeSafe(OnMessageReceived);
            }

            Session.Start();
        }

        private void OnSessionMessage(SessionMessage message)
        {
            if (message.Sender is WsSession session)
            {
                if (message.Content is Exception ex)
                {
                    if (OptionLogToConsole)
                        Console.WriteLine($"[Client {Id}] Exception: {ExceptionToString(ex)}");
                }
                else if (message.Content is SessionStatus status)
                {
                    if (status == SessionStatus.Started)
                    {
                        if (OptionLogToConsole)
                            Console.WriteLine($"[Client {Id}] Connected: {session}");
                    }
                    else if (status == SessionStatus.Stopped)
                    {
                        if (OptionLogToConsole)
                            Console.WriteLine($"[Client {Id}] Disconnected: {session}");
                    }
                }
            }
        }

        private void OnMessageReceived(ContentMessage<WsContent> message)
        {
            Console.ResetColor();

            if (message.IsIncoming)
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client {Id}] Received: {message.Content}");
            }
            else
            {
                if (OptionLogToConsole)
                    Console.WriteLine($"[Client {Id}] Sent: {message.Content}");
            }
        }

        public void Send(byte[] value)
        {
            if (IsConnected)
            {
                var content = new WsContent(value);

                Session.Send(content).ConfigureAwait(false);
            }
        }

        public void Send(string value)
        {
            if (IsConnected)
            {
                var content = new WsContent(value);

                Session.Send(content).ConfigureAwait(false);
            }
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (Status == SessionStatus.Started && !IsConnected)
                WaitForConnect();
        }

        public override void Dispose()
        {
            Stop();
        }
    }
}

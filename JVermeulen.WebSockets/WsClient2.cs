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
    public class WsClient2 : Actor
    {
        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public string ServerUrl { get; private set; }

        public WsSession2 Session { get; private set; }
        public bool IsConnected => Session != null && Session.IsConnected;

        public TimeSpan OptionConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public bool OptionReconnectOnHeatbeat { get; set; } = true;

        public WsClient2(ITcpEncoder<WsContent> encoder, string url)
        {
            Encoder = encoder;
            ServerUrl = url;
        }

        protected override void OnStarted()
        {
            WaitForConnectAsync().ConfigureAwait(false);
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Session?.Stop();
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
                Session = new WsSession2(Encoder, false, ServerUrl, context);
                Session.Outbox.SubscribeSafe(OnSessionMessage);
                Session.MessageBox.SubscribeSafe(OnMessageReceived);
            }

            Session.Start();
        }

        private void OnSessionMessage(SessionMessage message)
        {
            if (message.Sender is WsSession2 session)
            {
                if (message.Content is SessionStatus status)
                {
                    if (status == SessionStatus.Started)
                        Console.WriteLine($"[Client] Connected: {session}");
                    else if (status == SessionStatus.Stopped)
                        Console.WriteLine($"[Client] Disconnected: {session}");
                }
            }
        }

        private void OnMessageReceived(ContentMessage<WsContent> message)
        {
            Console.ResetColor();

            if (message.IsIncoming)
                Console.WriteLine($"[Client] Message received: {message.Content}");
            else
                Console.WriteLine($"[Client] Message sent: {message.Content}");
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

        public override void Dispose()
        {
            Stop();
        }
    }
}

using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsSession : Actor
    {
        /// <summary>
        /// A global unique Id.
        /// </summary>
        private static long GlobalSessionId;

        /// <summary>
        /// A unique Id for this session.
        /// </summary>
        public long SessionId { get; private set; }

        public bool IsServer { get; private set; }
        public string ServerUrl { get; private set; }
        public WebSocket Socket { get; private set; }
        private ArraySegment<byte> Buffer { get; set; }
        private TcpBuffer ReceiveBuffer { get; set; }

        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public MessageBox<ContentMessage<WsContent>> MessageBox { get; private set; }

        public bool IsConnected => Socket != null && Socket.State == WebSocketState.Open;

        public TimeSpan OptionReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3600);
        public TimeSpan OptionSendTimeout { get; set; } = TimeSpan.FromSeconds(15);
        public TimeSpan OptionPingInterval { get; set; } = TimeSpan.FromSeconds(15);
        public int OptionBufferSize { get; set; } = 8 * 1024;

        private CancellationTokenSource ReceiveCancellation { get; set; }

        public WsSession(ITcpEncoder<WsContent> encoder, bool isServer, string serverUrl, WebSocket socket) : base(TimeSpan.FromSeconds(60))
        {
            SessionId = Interlocked.Increment(ref GlobalSessionId);

            Socket = socket;
            Encoder = encoder;
            IsServer = isServer;
            ServerUrl = serverUrl;

            MessageBox = new MessageBox<ContentMessage<WsContent>>();
            Buffer = WebSocket.CreateServerBuffer(OptionBufferSize);
            ReceiveBuffer = new TcpBuffer();
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            WaitForReceive().ConfigureAwait(false);
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            ReceiveCancellation.Cancel();

            using (var timeout = new CancellationTokenSource(OptionSendTimeout))
            {
                Socket?.CloseAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
            }
        }

        private async Task WaitForReceive()
        {
            try
            {
                while (Status == SessionStatus.Started && IsConnected)
                {
                    WebSocketReceiveResult receiveResult;

                    ReceiveCancellation = new CancellationTokenSource(OptionReceiveTimeout);

                    receiveResult = await Socket.ReceiveAsync(Buffer, ReceiveCancellation.Token);

                    if (receiveResult.Count != 0 || receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        ReceiveBuffer.Add(Buffer, 0, receiveResult.Count);

                        if (receiveResult.EndOfMessage)
                            OnReceived(ReceiveBuffer);
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Stop();

                OnExceptionOccured(this, ex);
            }
        }

        private void OnReceived(TcpBuffer buffer)
        {
            while (Encoder.TryFindContent(buffer.Data, out WsContent content, out int numberOfBytes))
            {
                buffer.Remove(numberOfBytes);

                var message = new ContentMessage<WsContent>(SessionId.ToString(), ServerUrl, true, false, content, numberOfBytes - Encoder.NettoDelimeterLength);

                MessageBox.Add(message);
            }
        }

        public async Task<bool> Send(WsContent content)
        {
            try
            {
                if (!IsConnected)
                    return false;

                var data = Encoder.Encode(content);

                int index = 0;
                bool endOfMessage = false;

                while (!endOfMessage)
                {
                    var length = index + OptionBufferSize > data.Length ? data.Length - index : OptionBufferSize;
                    var frame = data.AsMemory(index, length);

                    index += OptionBufferSize;
                    endOfMessage = index >= data.Length;

                    using (var timeout = new CancellationTokenSource(OptionSendTimeout))
                    {
                        await Socket.SendAsync(data, content.IsText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, endOfMessage, timeout.Token);
                    }
                }

                var message = new ContentMessage<WsContent>(ServerUrl, SessionId.ToString(), false, false, content, data.Length);

                MessageBox.Add(message);

                return true;
            }
            catch (Exception ex)
            {
                OnExceptionOccured(this, ex);

                return false;
            }
        }

        private void OnExceptionOccured(object sender, Exception ex)
        {
            var message = new SessionMessage(this, ex);

            Outbox.Add(message);
        }

        public override string ToString()
        {
            return $"Session {SessionId} ({ServerUrl})";
        }

        public override void Dispose()
        {
            Stop();

            base.Dispose();
        }
    }
}

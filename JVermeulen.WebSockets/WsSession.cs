using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public bool ContentIsText { get; private set; }
        public string ServerUrl { get; private set; }
        public WebSocket Socket { get; private set; }
        private ArraySegment<byte> Buffer { get; set; }
        private TcpBuffer ReceiveBuffer { get; set; }

        public ITcpEncoder<Content> Encoder { get; private set; }
        public MessageBox<ContentMessage<Content>> MessageBox { get; private set; }

        public bool IsConnected => Socket != null && Socket.State == WebSocketState.Open;

        public TimeSpan OptionReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3600);
        public TimeSpan OptionSendTimeout { get; set; } = TimeSpan.FromSeconds(15);
        public int OptionBufferSize { get; set; } = 8 * 1024;

        public WsSession(ITcpEncoder<Content> encoder, bool isServer, bool contentIsText, string serverUrl, WebSocket socket) : base(TimeSpan.FromSeconds(60))
        {
            SessionId = Interlocked.Increment(ref GlobalSessionId);

            Socket = socket;
            Encoder = encoder;
            IsServer = isServer;
            ServerUrl = serverUrl;
            ContentIsText = contentIsText;

            MessageBox = new MessageBox<ContentMessage<Content>>();
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

            CloseAsync().Wait();
        }

        private async Task CloseAsync()
        {
            if (Socket.State == WebSocketState.Open || Socket.State == WebSocketState.CloseReceived)
            {
                using (var timeout = new CancellationTokenSource(OptionSendTimeout))
                {
                    if (IsServer)
                        await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
                    else
                        await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
                }
            }
        }

        private async Task WaitForReceive()
        {
            try
            {
                while (Status == SessionStatus.Started && IsConnected)
                {
                    WebSocketReceiveResult receiveResult;

                    using (var timeout = new CancellationTokenSource(OptionReceiveTimeout))
                    {
                        receiveResult = await Socket.ReceiveAsync(Buffer, timeout.Token);
                    }

                    if (receiveResult.Count > 0 && IsConnected)
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
                var aborted = FindExceptionRecursive(ex, out HttpListenerException innerException) && innerException.ErrorCode == (int)System.Net.Sockets.SocketError.OperationAborted;

                if (!aborted)
                    OnExceptionOccured(this, ex);

                Stop();
            }
        }

        private void OnReceived(TcpBuffer buffer)
        {
            while (Encoder.TryFindContent(buffer.Data, out Content content, out int numberOfBytes))
            {
                buffer.Remove(numberOfBytes);

                var message = new ContentMessage<Content>(SessionId.ToString(), ServerUrl, true, false, content, numberOfBytes - Encoder.NettoDelimeterLength);

                MessageBox.Add(message);
            }
        }

        public async Task<bool> Send(Content content)
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
                        await Socket.SendAsync(data, ContentIsText ? WebSocketMessageType.Text : WebSocketMessageType.Binary, endOfMessage, timeout.Token);
                    }
                }

                var message = new ContentMessage<Content>(ServerUrl, SessionId.ToString(), false, false, content, data.Length);

                MessageBox.Add(message);

                return true;
            }
            catch (Exception ex)
            {
                OnExceptionOccured(this, ex);

                return false;
            }
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

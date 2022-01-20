using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsSession : Actor
    {
        public TcpConnection Connection { get; set; }
        public WebSocket Socket { get; set; }
        private NetworkStream Stream { get; set; }
        private ArraySegment<byte> Buffer { get; set; }
        private TcpBuffer ReceiveBuffer { get; set; }

        public ITcpEncoder<WsContent> Encoder { get; private set; }
        public MessageBox<ContentMessage<WsContent>> MessageBox { get; private set; }

        public bool IsServer => Connection != null && Connection.IsServer;
        public bool IsConnected => Socket != null && Socket.State == WebSocketState.Open;
        public string LocalAddress => Connection?.LocalAddress;
        public string RemoteAddress => Connection?.RemoteAddress;

        public TimeSpan OptionReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3600);
        public TimeSpan OptionSendTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public WsSession(TcpConnection connection, ITcpEncoder<WsContent> encoder) : base(TimeSpan.FromSeconds(60))
        {
            Encoder = encoder;
            MessageBox = new MessageBox<ContentMessage<WsContent>>();

            Connection = connection;
            Connection.DataReceived += Connection_DataReceived;
            Connection.ExceptionOccured += OnExceptionOccured;

            Stream = new NetworkStream(Connection.Socket);
        }

        private void Connection_DataReceived(object sender, TcpBuffer e)
        {
            var request = Encoding.UTF8.GetString(e.Data.Span);

            if (WsHandshake.ValidateRequest(request, out string response, out Guid? requestId))
            {
                Connection.OptionReceiveEnabled = false;

                var data = Encoding.UTF8.GetBytes(response);

                Connection.Send(data);

                WaitForReceive().ConfigureAwait(false);
            }
            else
            {
                throw new WebSocketException(WebSocketError.NotAWebSocket);
            }
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            Connection?.Start();

            if (Socket == null)
                Socket = WebSocket.CreateFromStream(Stream, IsServer, null, TimeSpan.FromSeconds(60));

            Buffer = IsServer ? WebSocket.CreateServerBuffer(1024) : WebSocket.CreateClientBuffer(1024, 1024);
            ReceiveBuffer = new TcpBuffer();

            if (!IsServer)
                WaitForReceive().ConfigureAwait(false);
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Connection?.Stop();

            ReceiveBuffer.Dispose();
        }

        private async Task WaitForReceive()
        {
            try
            {
                if (IsConnected)
                {
                    WebSocketReceiveResult receiveResult;

                    using (var timeout = new CancellationTokenSource(OptionReceiveTimeout))
                    {
                        receiveResult = await Socket.ReceiveAsync(Buffer, timeout.Token);
                    }

                    if (receiveResult.Count != 0 || receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        ReceiveBuffer.Add(Buffer, 0, receiveResult.Count);

                        if (receiveResult.EndOfMessage)
                            OnReceived(ReceiveBuffer);

                        await WaitForReceive();
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                OnExceptionOccured(this, ex);
            }
        }

        private void OnReceived(TcpBuffer buffer)
        {
            while (Encoder.TryFindContent(buffer.Data, out WsContent content, out int numberOfBytes))
            {
                buffer.Remove(numberOfBytes);

                var message = new ContentMessage<WsContent>(RemoteAddress, LocalAddress, true, false, content, numberOfBytes - Encoder.NettoDelimeterLength);

                MessageBox.Add(message);
            }
        }

        public async Task Send(WsContent content)
        {
            var data = Encoder.Encode(content);

            using (var timeout = new CancellationTokenSource(OptionSendTimeout))
            {
                await Socket.SendAsync(data, content.MessageType, true, timeout.Token);
            }
        }

        private void OnExceptionOccured(object sender, Exception ex)
        {
            var message = new SessionMessage(this, ex);

            Outbox.Add(message);
        }

        public override string ToString()
        {
            if (IsServer)
                return $"WS Session ({LocalAddress} <= {RemoteAddress})";
            else
                return $"WS Session ({LocalAddress} => {RemoteAddress})";
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Connection != null)
            {
                Connection.DataReceived -= Connection_DataReceived;
                Connection.ExceptionOccured -= OnExceptionOccured;
            }
        }
    }
}

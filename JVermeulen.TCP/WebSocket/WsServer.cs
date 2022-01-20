using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.WebSocket
{
    public class WsServer : TcpServer<WsFrame>
    {
        private const string ServerKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private SHA1 CryptoServiceProvider { get; set; }

        public WsServer(ITcpEncoder<WsFrame> encoder, int port) : base(encoder, port)
        {
            CryptoServiceProvider = SHA1CryptoServiceProvider.Create();
        }

        public WsServer(ITcpEncoder<WsFrame> encoder, IPEndPoint serverEndpoint) : base(encoder, serverEndpoint)
        {
            CryptoServiceProvider = SHA1CryptoServiceProvider.Create();
        }

        protected override void OnTcpMessage(ContentMessage<WsFrame> message)
        {
            if (message.IsIncoming)
            {
                if (message.Content is WsFrame frame)
                {
                    if (frame.Opcode == WsFrameType.Handshake)
                        OnHandshake(message.SenderAddress, frame.ToString());
                    else if (frame.Opcode == WsFrameType.Text)
                        OnMessageReceived(message.SenderAddress, frame);
                }
            }
            else
            {
                //
            }
        }

        private void OnMessageReceived(string clientAddress, WsFrame frame)
        {
            Console.WriteLine(frame.ToString());

            if (OptionEchoMessages)
            {
                //WebSocket server must not use mask.
                frame.Mask = null;

                Send(frame, s => s.RemoteAddress == clientAddress);
            }

            Send(WsFrame.PingFrame, s => s.RemoteAddress == clientAddress);
        }

        protected Guid OnHandshake(string clientAddress, string content)
        {
            var clientKeyAsString = content.Replace("ey:", "`").Split('`')[1].Replace("\r", "").Split('\n')[0].Trim();
            var clientKeyAsBytes = Convert.FromBase64String(clientKeyAsString);
            var clientKeyAsGuid = new Guid(clientKeyAsBytes);

            var acceptKey = AcceptKey(clientKeyAsString, ServerKey);
            var response = AcceptResponse(acceptKey);
            var message = new WsFrame
            {
                Opcode = WsFrameType.Handshake,
                Payload = Encoding.UTF8.GetBytes(response),
            };

            Send(message, s => s.RemoteAddress == clientAddress);

            return clientKeyAsGuid;
        }

        private string AcceptKey(string clientKey, string serverKey)
        {
            var data = Encoding.UTF8.GetBytes(clientKey + serverKey);
            var hash = CryptoServiceProvider.ComputeHash(data);

            return Convert.ToBase64String(hash);
        }

        private string AcceptResponse(string acceptKey)
        {
            var builder = new StringBuilder();
            builder.AppendLine("HTTP/1.1 101 Switching Protocols");
            builder.AppendLine("Connection: upgrade");
            builder.AppendLine("Upgrade: websocket");
            builder.AppendLine("Sec-WebSocket-Accept: " + acceptKey);
            builder.AppendLine();

            return builder.ToString();
        }
    }
}

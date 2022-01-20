using System;
using System.Net.WebSockets;
using System.Text;

namespace JVermeulen.WebSockets
{
    public class WsContent
    {
        public readonly WebSocketMessageType MessageType;
        public readonly byte[] Binary;
        public readonly string Text;

        public WsContent(byte[] data, WebSocketMessageType messageType)
        {
            if (messageType == WebSocketMessageType.Close)
                throw new NotSupportedException($"Unable to create WsContent. MessageType '{messageType}' is not suppored.");

            MessageType = messageType;

            Binary = data ?? throw new ArgumentNullException(nameof(data));

            if (MessageType == WebSocketMessageType.Text)
                Text = Encoding.UTF8.GetString(data);
        }

        public WsContent(string content)
        {
            MessageType = WebSocketMessageType.Text;

            Text = content;

            Binary = Encoding.UTF8.GetBytes(content);
        }

        public override string ToString()
        {
            if (MessageType == WebSocketMessageType.Text)
                return Text;
            else
                return $"Binary ({Binary.Length} bytes)";
        }
    }
}

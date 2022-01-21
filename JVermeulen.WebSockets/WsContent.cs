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

        public WsContent(byte[] data, bool isText = false)
        {
            MessageType = isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;

            Binary = data ?? throw new ArgumentNullException(nameof(data));

            if (isText)
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
                return $"Text ({Binary.Length} bytes)";
            else
                return $"Binary ({Binary.Length} bytes)";
        }
    }
}

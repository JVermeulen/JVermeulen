using JVermeulen.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
    public class WsEncoder : ITcpEncoder<WsContent>
    {
        public static WsEncoder TextEncoder = new WsEncoder(WebSocketMessageType.Text);
        public static WsEncoder BinaryEncoder = new WsEncoder(WebSocketMessageType.Binary);

        public WebSocketMessageType MessageType { get; set; }

        public Encoding Encoding { get; private set; }
        public int NettoDelimeterLength => 0;

        public WsEncoder(WebSocketMessageType messageType)
        {
            MessageType = messageType;

            Encoding = Encoding.UTF8;
        }

        public byte[] Encode(WsContent value)
        {
            return value.Binary;
        }

        public WsContent Decode(byte[] data)
        {
            return new WsContent(data, MessageType);
        }

        public bool TryFindContent(Memory<byte> buffer, out WsContent content, out int numberOfBytes)
        {
            content = null;
            numberOfBytes = 0;

            if (buffer.Length > 0)
            {
                content = Decode(buffer.ToArray());

                numberOfBytes = buffer.Length;
            }

            return content != null;
        }
    }
}

using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;

namespace JVermeulen.WebSockets
{
    public class WsContent
    {
        public readonly bool IsText;
        public readonly byte[] Binary;
        public readonly string Text;

        public WsContent(byte[] data, bool isText = false)
        {
            IsText = isText;

            Binary = data ?? throw new ArgumentNullException(nameof(data));

            if (IsText)
                Text = Encoding.UTF8.GetString(data);
        }

        public WsContent(string content)
        {
            IsText = true;

            Text = content;

            Binary = Encoding.UTF8.GetBytes(content);
        }

        public override string ToString()
        {
            return $"{Binary.Length} bytes";
        }
    }
}

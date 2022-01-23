using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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

        public bool TryGetValueAs<T>(out T value)
        {
            value = default;

            try
            {
                if (typeof(T) == typeof(byte[]))
                {
                    value = (T)Convert.ChangeType(Binary, typeof(T));

                    return true;
                }
                else if (typeof(T) == typeof(string))
                {
                    value = (T)Convert.ChangeType(Text, typeof(T));

                    return true;
                }
                else if (typeof(T) == typeof(JsonDocument))
                {
                    value = (T)Convert.ChangeType(JsonDocument.Parse(Text), typeof(T));

                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override string ToString()
        {
            if (IsText)
                return $"{Binary.Length} bytes - {Text}";
            else
                return $"{Binary.Length} bytes - Binary";
        }
    }
}

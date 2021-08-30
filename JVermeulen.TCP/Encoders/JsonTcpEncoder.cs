using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Encoders
{
    public class JsonTcpEncoder : ITcpEncoder<string>
    {
        public static JsonTcpEncoder UTF8Encoder = new JsonTcpEncoder(Encoding.UTF8);
        public int DelimeterNettoLength => 0;

        public Encoding Encoding { get; private set; }

        public JsonTcpEncoder(Encoding encoding)
        {
            Encoding = encoding;
        }

        public byte[] Encode(string value)
        {
            return Encoding.GetBytes((string)value);
        }

        public string Decode(byte[] data)
        {
            return Encoding.GetString(data);
        }

        public bool TryFindContent(byte[] data, out string content, out byte[] nextContent)
        {
            content = string.Empty;
            nextContent = Array.Empty<byte>();

            try
            {
                var text = Encoding.GetString(data);

                if (TryFindContent(text, out string message, out string restString))
                {
                    content = message;
                    nextContent = Encoding.GetBytes(restString);

                    return true;
                }
            }
            catch
            {
                //
            }

            return false;
        }

        public bool TryFindContent(string content, out string message, out string nextMessage)
        {
            message = null;
            nextMessage = string.Empty;

            try
            {
                int level = 0;

                for (int i = 0; i < content.Length; i++)
                {
                    if (content[i] == '{')
                        level++;
                    else if (content[i] == '}')
                        level--;

                    if (level == 0)
                    {
                        message = content.Substring(0, i + 1);
                        nextMessage = content.Substring(i + 1, content.Length - i - 1);

                        return true;
                    }
                }
            }
            catch
            {
                //
            }

            return false;
        }

        public bool TryFindContent(TcpBuffer buffer, out string content, out int numberOfBytes)
        {
            throw new NotImplementedException();
        }
    }
}

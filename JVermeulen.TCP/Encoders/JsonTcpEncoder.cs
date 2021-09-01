using System;
using System.Text;

namespace JVermeulen.TCP.Encoders
{
    public class JsonTcpEncoder : ITcpEncoder<string>
    {
        public static JsonTcpEncoder UTF8Encoder = new JsonTcpEncoder(Encoding.UTF8);
        public int NettoDelimeterLength => 0;
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

        public bool TryFindContent(Memory<byte> buffer, out string content, out int numberOfBytes)
        {
            content = null;
            numberOfBytes = 0;

            try
            {
                var beginMarker = Encoding.GetBytes("{");
                var endMarker = Encoding.GetBytes("}");

                int level = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (ByteTcpEncoder.Match(buffer, beginMarker, i))
                        level++;
                    else if (ByteTcpEncoder.Match(buffer, endMarker, i))
                        level--;

                    if (level == 0)
                    {
                        numberOfBytes = i + endMarker.Length;

                        var data = buffer.Slice(0, numberOfBytes);
                        content = Encoding.GetString(data.Span);

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
    }
}

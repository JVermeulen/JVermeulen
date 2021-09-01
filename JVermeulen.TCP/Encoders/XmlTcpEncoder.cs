using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JVermeulen.TCP.Encoders
{
    public class XmlTcpEncoder : ITcpEncoder<string>
    {
        public static XmlTcpEncoder UTF8Encoder = new XmlTcpEncoder(Encoding.UTF8);
        public int NettoDelimeterLength => 0;

        public Encoding Encoding { get; private set; }

        public XmlTcpEncoder(Encoding encoding)
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
            content = string.Empty;
            numberOfBytes = 0;

            try
            {
                var text = Encoding.GetString(buffer.Span);

                if (TryFindContent(text, out string message, out string restString))
                {
                    content = message;
                    numberOfBytes = Encoding.GetBytes(content).Length;

                    return true;
                }
            }
            catch
            {
                //
            }

            return false;
        }

        public bool TryFindContent(byte[] buffer, out string content, out byte[] nextContent)
        {
            content = string.Empty;
            nextContent = Array.Empty<byte>();

            try
            {
                var text = Encoding.GetString(buffer);

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

        public bool TryFindContent(string buffer, out string message, out string nextMessage)
        {
            message = null;
            nextMessage = string.Empty;

            try
            {
                // Parse full content
                if (IsXmlContent(buffer))
                {
                    message = buffer;

                    return true;
                }

                // Parse parts of content
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == '>')
                    {
                        var potentialMessage = buffer.Substring(0, i + 1);

                        if (IsXmlContent(potentialMessage))
                        {
                            nextMessage = buffer.Substring(i + 1, buffer.Length - i - 1);

                            message = potentialMessage;

                            return true;
                        }
                    }
                }
            }
            catch
            {
                //
            }

            return false;
        }

        private static bool IsXmlContent(string content)
        {
            try
            {
                if (content == null)
                    return false;
                else if (!content.StartsWith("<"))
                    return false;

                var document = XDocument.Parse(content);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

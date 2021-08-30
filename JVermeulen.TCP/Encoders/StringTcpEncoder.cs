using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Encoders
{
    public class StringTcpEncoder : ITcpEncoder<string>
    {
        public static StringTcpEncoder NewLineUTF8Encoder = new StringTcpEncoder(Encoding.UTF8, Environment.NewLine, false);
        public static StringTcpEncoder NullByteUTF8Encoder = new StringTcpEncoder(Encoding.UTF8, "\0", false);

        public Encoding Encoding { get; private set; }
        public string Delimeter { get; private set; }
        public bool DelimeterIsPartOfMessage { get; private set; }
        private byte[] DelimeterBytes { get; set; }
        public int DelimeterNettoLength => DelimeterIsPartOfMessage ? 0 : DelimeterBytes.Length;

        public StringTcpEncoder(Encoding encoding, string delimeter, bool delimeterIsPartOfMessage = false)
        {
            Encoding = encoding;
            Delimeter = delimeter;
            DelimeterIsPartOfMessage = delimeterIsPartOfMessage;
            DelimeterBytes = Encoding.GetBytes(Delimeter);
        }

        public byte[] Encode(string value)
        {
            if (DelimeterIsPartOfMessage)
                return Encoding.GetBytes(value);
            else
                return Encoding.GetBytes(value + Delimeter);
        }

        public string Decode(byte[] data)
        {
            if (DelimeterIsPartOfMessage)
                return Encoding.GetString(data);
            else
                return Encoding.GetString(data.Take(data.Length - DelimeterBytes.Length).ToArray());
        }

        public bool TryFindContent(byte[] buffer, out string content, out byte[] nextContent)
        {
            content = null;
            nextContent = Array.Empty<byte>();

            try
            {
                var index = ByteTcpEncoder.Search(buffer, DelimeterBytes);

                if (index > -1)
                {
                    if (DelimeterIsPartOfMessage)
                    {
                        content = Encoding.GetString(buffer.Take(index + DelimeterBytes.Length).ToArray());
                        nextContent = buffer.Skip(index + DelimeterBytes.Length).Take(buffer.Length - index + DelimeterBytes.Length).ToArray();
                    }
                    else
                    {
                        content = Encoding.GetString(buffer.Take(index).ToArray());
                        nextContent = buffer.Skip(index + DelimeterBytes.Length).Take(buffer.Length - index).ToArray();
                    }

                    return true;
                }
            }
            catch
            {
                //
            }

            return false;
        }

        private int Search(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (match(haystack, needle, i))
                    return i;
            }

            return -1;
        }

        private bool match(byte[] haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack[i + start])
                        return false;
                }

                return true;
            }
        }

        public bool TryFindContent(TcpBuffer buffer, out string content, out int numberOfBytes)
        {
            throw new NotImplementedException();
        }
    }
}

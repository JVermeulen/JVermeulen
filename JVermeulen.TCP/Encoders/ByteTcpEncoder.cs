using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Encoders
{
    public class ByteTcpEncoder : ITcpEncoder<byte[]>
    {
        public static ByteTcpEncoder NullByteEncoder = new ByteTcpEncoder(new byte[] { 0 }, false);

        public byte[] Delimeter { get; private set; }
        public bool DelimeterIsPartOfMessage { get; private set; }

        public ByteTcpEncoder(byte[] delimeter, bool delimeterIsPartOfMessage = false)
        {
            Delimeter = delimeter;
            DelimeterIsPartOfMessage = delimeterIsPartOfMessage;
        }

        public byte[] Encode(byte[] value)
        {
            return DelimeterIsPartOfMessage ? value : value.Concat(Delimeter).ToArray();
        }

        public byte[] Decode(byte[] data)
        {
            return DelimeterIsPartOfMessage ? data : data.Take(data.Length - Delimeter.Length).ToArray();
        }

        public bool TryFindContent(byte[] buffer, out byte[] content, out byte[] nextContent)
        {
            content = Array.Empty<byte>();
            nextContent = Array.Empty<byte>();

            try
            {
                var index = Search(buffer, Delimeter);

                if (index > -1)
                {
                    if (DelimeterIsPartOfMessage)
                    {
                        content = buffer.Take(index + Delimeter.Length).ToArray();
                        nextContent = buffer.Skip(index + Delimeter.Length).Take(buffer.Length - index + Delimeter.Length).ToArray();
                    }
                    else
                    {
                        content = buffer.Take(index).ToArray();
                        nextContent = buffer.Skip(index + Delimeter.Length).Take(buffer.Length - index).ToArray();
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

        public static int Search(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (Match(haystack, needle, i))
                    return i;
            }

            return -1;
        }

        private static bool Match(byte[] haystack, byte[] needle, int start)
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

    }
}

using System;
using System.Linq;

namespace JVermeulen.TCP.Encoders
{
    public class ByteTcpEncoder : ITcpEncoder<byte[]>
    {
        public static ByteTcpEncoder NullByteEncoder = new ByteTcpEncoder(new byte[] { 0 }, false);

        public byte[] Delimeter { get; private set; }
        public bool DelimeterIsPartOfMessage { get; private set; }
        public int NettoDelimeterLength => DelimeterIsPartOfMessage ? 0 : Delimeter.Length;

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

        public bool TryFindContent(Memory<byte> buffer, out byte[] content, out int numberOfBytes)
        {
            content = Array.Empty<byte>();
            numberOfBytes = 0;

            try
            {
                var index = Search(buffer, Delimeter);

                if (index > -1)
                {
                    var length = DelimeterIsPartOfMessage ? index + Delimeter.Length : index;
                    content = buffer.Slice(0, length).ToArray();

                    numberOfBytes = index + Delimeter.Length;

                    return true;
                }
            }
            catch
            {
                //
            }

            return false;
        }

        public static int Search(Memory<byte> haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (Match(haystack, needle, i))
                    return i;
            }

            return -1;
        }

        public static bool Match(Memory<byte> haystack, byte[] needle, int start)
        {
            if (needle.Length + start > haystack.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < needle.Length; i++)
                {
                    if (needle[i] != haystack.Span[i + start])
                        return false;
                }

                return true;
            }
        }
    }
}

using JVermeulen.TCP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Encoders
{
    public class StringTcpEncoder : ITcpEncoder<string>
    {
        public static StringTcpEncoder NewLineUTF8Encoder = new StringTcpEncoder(Encoding.UTF8, "\r\n", false);
        public static StringTcpEncoder NullByteUTF8Encoder = new StringTcpEncoder(Encoding.UTF8, "\0", false);

        public Encoding Encoding { get; private set; }
        public string Delimeter { get; private set; }
        public bool DelimeterIsPartOfMessage { get; private set; }
        private byte[] DelimeterBytes { get; set; }
        public int NettoDelimeterLength => DelimeterIsPartOfMessage ? 0 : DelimeterBytes.Length;

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

        public bool TryFindContent(Memory<byte> buffer, out string content, out int numberOfBytes)
        {
            content = null;
            numberOfBytes = 0;

            try
            {
                var index = ByteTcpEncoder.Search(buffer, DelimeterBytes);

                if (index > -1)
                {
                    numberOfBytes = index + DelimeterBytes.Length;

                    var data = DelimeterIsPartOfMessage ? buffer.Slice(0, index + DelimeterBytes.Length) : buffer.Slice(0, index);
                    content = Encoding.GetString(data.Span);

                    return true;
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

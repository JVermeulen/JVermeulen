using System;

namespace JVermeulen.TCP
{
    public interface ITcpEncoder<T>
    {
        byte[] Encode(T value);
        T Decode(byte[] data);

        int DelimeterNettoLength { get; }

        bool TryFindContent(byte[] buffer, out T content, out byte[] nextContent);

        bool TryFindContent(TcpBuffer buffer, out T content, out int numberOfBytes);
    }
}

using JVermeulen.TCP.Core;
using System;

namespace JVermeulen.TCP
{
    public interface ITcpEncoder<T>
    {
        byte[] Encode(T value);
        T Decode(byte[] data);

        int NettoDelimeterLength { get; }

        bool TryFindContent(Memory<byte> buffer, out T content, out int numberOfBytes);
    }
}

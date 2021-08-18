using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    public interface ITcpEncoder<T>
    {
        byte[] Encode(T value);
        T Decode(byte[] data);

        bool TryFindContent(byte[] buffer, out T content, out byte[] nextContent);
    }
}

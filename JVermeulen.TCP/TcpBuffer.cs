using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    /// <summary>
    /// The session receive state.
    /// </summary>
    public class TcpBuffer
    {
        /// <summary>
        /// The size of the receive buffer.
        /// </summary>
        public const int BufferSize = 1024 * 8;

        /// <summary>
        /// The receive buffer.
        /// </summary>
        public byte[] Buffer = new byte[BufferSize];

        /// <summary>
        /// The builder of the content.
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        public TcpBuffer(byte[] data = null)
        {
            Data = data ?? Array.Empty<byte>();
        }

        /// <summary>
        /// Adds the current buffer to existing data.
        /// </summary>
        /// <param name="length">The length of data read.</param>
        public void AddBufferToData(int length)
        {
            if (length > 0)
            {
                var data = Buffer.Take(length).ToArray();

                if (Data.Length == 0)
                    Data = data;
                else
                    Data = Data.Concat(data).ToArray();
            }
        }
    }
}

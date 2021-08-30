using System;
using System.Buffers;

namespace JVermeulen.TCP
{
    /// <summary>
    /// Memory friendly, expendable byte[] buffer. Uses the ArrayPool so always dispose after use.
    /// </summary>
    public class TcpBuffer : IDisposable
    {
        /// <summary>
        /// The minimum size of the buffer created. Default is 1024.
        /// </summary>
        public int OptionMinimumBufferLength { get; set; } = 1024;

        /// <summary>
        /// The internal buffer.
        /// </summary>
        private byte[] Buffer { get; set; }

        /// <summary>
        /// The start of the relevant data.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The length of the relevant data.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// The relevant data in the buffer.
        /// </summary>
        public Memory<byte> Data { get; private set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        public TcpBuffer()
        {
            Buffer = GetNewBuffer(0);

            Index = 0;
            Length = 0;
            Data = new Memory<byte>(Buffer, Index, Length);
        }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="array">The initial buffer to use. A copy of this data will be made.</param>
        public TcpBuffer(byte[] array) : this(array, 0, array.Length)
        {
            //
        }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="array">The initial buffer to use. A copy of this data will be made.</param>
        /// <param name="index">The start of the data in the buffer.</param>
        /// <param name="length">The length of the data in the buffer.</param>
        public TcpBuffer(byte[] array, int index, int length)
        {
            Buffer = GetNewBuffer(length);
            Array.Copy(array, index, Buffer, index, length);

            Index = 0;
            Length = length;
            Data = new Memory<byte>(Buffer, index, length);
        }

        /// <summary>
        /// Returns a new buffer from the ArrayPool.
        /// </summary>
        /// <param name="length">The minimum length of the buffer to request.</param>
        private byte[] GetNewBuffer(int length)
        {
            if (length < OptionMinimumBufferLength)
                length = OptionMinimumBufferLength;

            return ArrayPool<byte>.Shared.Rent(length);
        }

        /// <summary>
        /// Add the given buffer to the existing one.
        /// </summary>
        /// <param name="array">The buffer to add. A copy of this data will be made.</param>
        /// <param name="index">The start of the data in the buffer.</param>
        /// <param name="length">The length of the data in the buffer.</param>
        public void Add(byte[] array, int index, int length)
        {
            if (Buffer.Length - Index - Length >= length)
            {
                Array.Copy(array, index, Buffer, Index + Length, length);
            }
            else
            {
                var buffer = GetNewBuffer(Length + length);

                Array.Copy(Buffer, Index, buffer, 0, Length);
                Array.Copy(array, index, buffer, Length, length);

                ArrayPool<byte>.Shared.Return(Buffer);
                Buffer = buffer;

                Index = 0;
            }

            Length += length;
            Data = new Memory<byte>(Buffer, Index, Length);
        }

        /// <summary>
        /// Remove the given number of bytes from the start of the buffer.
        /// </summary>
        /// <param name="length">The number of bytes to remove.</param>
        public void Remove(int length)
        {
            Index += length;
            Length -= length;

            Data = new Memory<byte>(Buffer, Index, Length);
        }

        /// <summary>
        /// Disposes this object. Returns the buffer to the ArrayPool.
        /// </summary>
        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}

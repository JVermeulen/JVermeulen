using JVermeulen.TCP.WebSocket;
using System;
using System.Buffers.Binary;
using System.Text;

namespace JVermeulen.TCP.Encoders
{
    public class WsTcpEncoder : ITcpEncoder<WsFrame>
    {
        public static WsTcpEncoder Default = new WsTcpEncoder();

        public Encoding Encoding { get; private set; }
        private byte[] HandshakeKey { get; set; }
        public int NettoDelimeterLength => 0;

        public WsTcpEncoder()
        {
            Encoding = Encoding.UTF8;
            HandshakeKey = Encoding.GetBytes("GET");
        }

        public byte[] Encode(WsFrame value)
        {
            return Parse(value);
        }

        public WsFrame Decode(byte[] data)
        {
            return Parse(data);
        }

        public bool TryFindContent(Memory<byte> buffer, out WsFrame content, out int numberOfBytes)
        {
            content = null;
            numberOfBytes = 0;

            if (buffer.Length < 3)
                return false;

            if (buffer.Span.StartsWith(HandshakeKey))
            {
                content = new WsFrame
                {
                    Opcode = WsFrameType.Handshake,
                    Payload = buffer.ToArray(),
                };

                numberOfBytes = buffer.Length;
            }
            else
            {
                content = Parse(buffer.Span);

                numberOfBytes = (int)content.TotalSizeInBytes;
            }

            return true;
        }

        public static byte[] Parse(WsFrame frame)
        {
            if (frame.Opcode == WsFrameType.Handshake)
                return frame.Payload;

            var index = 0;
            var data = new byte[frame.TotalSizeInBytes];

            data[index] = 0b0_0_0_0_0000; // FIN (1 byte) | RSV1 (1 byte) | RSV2 (1 byte) | RSV3 (1 byte) | Opcode (4 bytes) 
            if (frame.FIN)
                data[index] |= 0b1_0_0_0_0000;
            if (frame.RSV1)
                data[index] |= 0b0_1_0_0_0000;
            if (frame.RSV2)
                data[index] |= 0b0_0_1_0_0000;
            if (frame.RSV3)
                data[index] |= 0b0_0_0_1_0000;
            if (frame.Opcode > 0)
                data[index] += (byte)frame.Opcode;
            index++;

            if (frame.Payload == null)
                return data;

            SplitPayloadLength(frame.Payload.Length, out byte payloadSize, out UInt16? extended1, out UInt64? extended2);

            data[index] = 0b0_0000000; // Mask (1 byte) | Size (7 bytes)
            if (frame.Mask != null)
                data[index] |= 0b1_0000000;
            data[index] |= payloadSize;
            index++;

            if (extended1.HasValue)
            {
                // Extended size (2 bytes)
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(index), extended1.Value);

                index += 2;
            }

            if (extended2.HasValue)
            {
                // Extended size (4 bytes)
                BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(index), extended1.Value);

                index += 4;
            }

            if (frame.Mask != null)
            {
                // Mask (4 bytes)
                Array.Copy(frame.Mask, 0, data, index, frame.Mask.Length);

                index += frame.Mask.Length;
            }

            if (frame.Payload != null)
            {
                // Payload (variable)
                Array.Copy(frame.Payload, 0, data, index, frame.Payload.Length);
            }

            if (frame.Mask != null)
            {
                for (int i = 0; i < frame.Payload.Length; i++)
                {
                    data[index] = (byte)(data[index] ^ frame.Mask[i % 4]);

                    index++;
                }
            }

            return data;
        }

        public static WsFrame Parse(Span<byte> data)
        {
            int index = 2;
            int maskSize = 4;

            var frame = new WsFrame
            {
                FIN = (data[0] & 0x80) == 0x80,
                RSV1 = (data[0] & 0x40) == 0x40,
                RSV2 = (data[0] & 0x20) == 0x20,
                RSV3 = (data[0] & 0x10) == 0x10,
                Opcode = (WsFrameType)(data[0] & 0x0f),

                Mask = (data[1] & 0x80) == 0x80 ? new byte[maskSize] : null,
            };

            long payloadLength = (byte)(data[1] & 0x7f);

            if (frame.RSV1)
                throw new NotSupportedException($"Unable to parse WebSocket frame. RSV1 is False.");
            if (frame.RSV2)
                throw new NotSupportedException($"Unable to parse WebSocket frame. RSV2 is False.");
            if (frame.RSV3)
                throw new NotSupportedException($"Unable to parse WebSocket frame. RSV3 is False.");

            if (payloadLength == 126)
            {
                payloadLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(index));

                index += 2;
            }
            else if (payloadLength == 127)
            {
                payloadLength = BinaryPrimitives.ReadInt64BigEndian(data.Slice(index));

                index += 8;
            }

            //Read mask if set in header
            if (frame.Mask != null)
            {
                frame.Mask = data.Slice(index, maskSize).ToArray();

                index += maskSize;
            }

            //Validate max PayloadLength
            if (payloadLength > int.MaxValue)
                throw new NotSupportedException($"Unable to parse WebSocket frame. PayloadLength ({payloadLength}) is higher then {int.MaxValue}.");

            //Read content and decode with mask if set in header
            frame.Payload = data.Slice(index, (int)payloadLength).ToArray();

            //Unmaskif set in header
            if (frame.Mask != null)
            {
                for (long i = 0; i < payloadLength; i++)
                    frame.Payload[i] = (byte)(frame.Payload[i] ^ frame.Mask[i % 4]);
            }

            var totalSizeInBytes = index + (int)payloadLength;
            if (frame.TotalSizeInBytes != totalSizeInBytes)
                throw new ApplicationException($"Unable to parse WebSocket frame. Calculated size ({frame.TotalSizeInBytes}) does not equal frame size ({totalSizeInBytes}).");

            return frame;
        }

        public static void SplitPayloadLength(long payloadLength, out byte length, out UInt16? extended1, out UInt64? extended2)
        {
            if (payloadLength > UInt16.MaxValue)
            {
                length = 127;
                extended1 = null;
                extended2 = (UInt64)payloadLength;
            }
            else if (payloadLength > 125)
            {
                length = 126;
                extended1 = (UInt16)payloadLength;
                extended2 = null;
            }
            else
            {
                length = (byte)payloadLength;
                extended1 = null;
                extended2 = null;
            }
        }
    }
}

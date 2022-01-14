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
            if (value.Opcode == WsFrameType.Handshake)
                return value.Payload;
            else
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

            var span = buffer.Span;
            if (span.StartsWith(HandshakeKey))
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
                content = Parse(span);

                numberOfBytes = (int)content.TotalSizeInBytes;
            }

            return true;
        }

        public static byte[] Parse(WsFrame frame)
        {
            SplitPayloadLength(frame.PayloadLength, out byte length, out UInt16? extended1, out UInt64? extended2);

            var index = 0;
            var size = frame.CalculateTotalSizeInBytes();
            var data = new byte[size];

            data[index] = 0b0_0_0_0_0000; // fin | rsv1 | rsv2 | rsv3 | [ OPCODE | OPCODE | OPCODE | OPCODE ]
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

            data[index] = 0b0_0000000;
            if (frame.Mask != null)
                data[index] |= 0b1_0000000;
            data[index] |= length;
            index++;

            if (extended1.HasValue)
            {
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(index), extended1.Value);

                index += 2;
            }

            if (extended2.HasValue)
            {
                BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(index), extended1.Value);

                index += 4;
            }

            if (frame.Mask != null)
            {
                Array.Copy(frame.Mask, 0, data, index, frame.Mask.Length);

                index += frame.Mask.Length;
            }

            if (frame.Payload != null)
            {
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
                PayloadLength = (byte)(data[1] & 0x7f),
            };

            if (frame.RSV1)
                throw new NotSupportedException($"Unable to parse WebSocket frame. RSV1 is False.");
            if (frame.RSV2)
                throw new NotSupportedException($"Unable to parse WebSocket frame. RSV2 is False.");
            if (frame.RSV3)
                throw new NotSupportedException($"Unable to parse WebSocket frame. RSV3 is False.");

            if (frame.PayloadLength == 126)
            {
                frame.PayloadLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(index));

                index += 2;
            }
            else if (frame.PayloadLength == 127)
            {
                frame.PayloadLength = BinaryPrimitives.ReadInt64BigEndian(data.Slice(index));

                index += 8;
            }

            //Read mask if set in header
            if (frame.Mask != null)
            {
                frame.Mask = data.Slice(index, maskSize).ToArray();

                index += maskSize;
            }

            //Validate max PayloadLength
            if (frame.PayloadLength > int.MaxValue)
                throw new NotSupportedException($"Unable to parse WebSocket frame. PayloadLength ({frame.PayloadLength}) is higher then {int.MaxValue}.");

            //Read content and decode with mask if set in header
            frame.Payload = data.Slice(index, (int)frame.PayloadLength).ToArray();

            //Unmaskif set in header
            if (frame.Mask != null)
            {
                for (long i = 0; i < frame.PayloadLength; i++)
                    frame.Payload[i] = (byte)(frame.Payload[i] ^ frame.Mask[i % 4]);
            }

            frame.TotalSizeInBytes = index + (int)frame.PayloadLength;

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

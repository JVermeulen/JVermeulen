using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.WebSocket
{
    public class WsFrame
    {
        public static WsFrame PingFrame = new WsFrame { FIN = true, Opcode = WsFrameType.Ping };

        /// <summary>
        /// Indicates that this is the final fragment in a message.  The first fragment MAY also be the final fragment.
        /// </summary>
        public bool FIN { get; set; }

        /// <summary>
        /// MUST be 0 unless an extension is negotiated that defines meanings for non-zero values.If a nonzero value is received and none of the 
        /// negotiated extensions defines the meaning of such a nonzero value, the receiving endpoint MUST Fail the WebSocket Connection.
        /// </summary>
        public bool RSV1 { get; set; }

        /// <summary>
        /// MUST be 0 unless an extension is negotiated that defines meanings for non-zero values.If a nonzero value is received and none of the 
        /// negotiated extensions defines the meaning of such a nonzero value, the receiving endpoint MUST Fail the WebSocket Connection.
        /// </summary>
        public bool RSV2 { get; set; }

        /// <summary>
        /// MUST be 0 unless an extension is negotiated that defines meanings for non-zero values.If a nonzero value is received and none of the 
        /// negotiated extensions defines the meaning of such a nonzero value, the receiving endpoint MUST Fail the WebSocket Connection.
        /// </summary>
        public bool RSV3 { get; set; }

        /// <summary>
        /// Defines the interpretation of the "Payload data". If an unknown opcode is received, the receiving endpoint MUST Fail the WebSocket Connection.
        /// </summary>
        public WsFrameType Opcode { get; set; }

        /// <summary>
        /// All frames sent from the client to the server are masked by a 32-bit value that is contained within the frame.This field is present if 
        /// the mask bit is set to 1 and is absent if the mask bit is set to 0.  See Section 5.3 for further information on client- to-server masking.
        /// </summary>
        public byte[] Mask { get; set; }

        /// <summary>
        /// The "Payload data" is defined as "Extension data" concatenated with "Application data".
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Represents the text value of this object.
        /// </summary>
        public override string ToString()
        {
            return Encoding.UTF8.GetString(Payload);
        }

        /// <summary>
        /// Returns the size of the frame header, not including the mask.
        /// </summary>
        public int HeaderSizeInBytes
        {
            get
            {
                if (Payload == null)
                    return 1;
                else if (Payload.Length > UInt16.MaxValue)
                    return 6;
                else if (Payload.Length > 125)
                    return 4;
                else
                    return 2;
            }
        }

        /// <summary>
        /// Returns the size of the frame mask, or 0 if not available.
        /// </summary>
        public int MaskSizeInBytes => Mask != null ? Mask.Length : 0;

        /// <summary>
        /// Returns the total size of the frame.
        /// </summary>
        public long TotalSizeInBytes
        {
            get
            {
                if (Opcode == WsFrameType.Handshake)
                    return Payload.Length;
                else if (Payload != null)
                    return HeaderSizeInBytes + MaskSizeInBytes + Payload.Length;
                else
                    return HeaderSizeInBytes;
            }
        }

        public string Print()
        {
            Encoders.WsTcpEncoder.SplitPayloadLength(Payload.Length, out byte length, out UInt16? extended1, out UInt64? extended2);

            var extended1T = extended1.HasValue ? $"{extended1.Value:D31}" : new string(' ', 31);
            var extended2T = extended2.HasValue ? $"{extended2.Value:D63}" : new string(' ', 63);
            var maskT = Mask != null ? new string(' ', 55) + Convert.ToHexString(Mask) : new string(' ', 63);

            var builder = new StringBuilder();
            builder.AppendLine(" 0                   1                   2                   3  ");
            builder.AppendLine(" 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1");
            builder.AppendLine("+-+-+-+-+-------+-+-------------+-------------------------------+");
            builder.AppendLine($"|{(FIN ? 1 : 0)}|{(RSV1 ? 1 : 0)}|{(RSV2 ? 1 : 0)}|{(RSV3 ? 1 : 0)}|{(byte)Opcode:D7}|{(Mask != null ? 1 : 0)}|{length:D13}|{extended1T}|");
            builder.AppendLine($"|{extended2T}|");
            builder.AppendLine("+-+-+-+-+-------+-+-------------+-------------------------------+");
            builder.AppendLine($"|{maskT}|");
            builder.AppendLine("+---------------------------------------------------------------+");

            return builder.ToString();
        }
    }
}
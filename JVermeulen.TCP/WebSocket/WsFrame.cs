using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP.WebSocket
{
    public class WsFrame
    {
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
        ///  The length of the "Payload data", in bytes. The payload length is the length of the "Extension data" + the length of the "Application data".
        ///  The length of the "Extension data" may be zero, in which case the payload length is the length of the "Application data".
        /// </summary>
        public long PayloadLength { get; set; }

        /// <summary>
        /// The "Payload data" is defined as "Extension data" concatenated with "Application data".
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// The size in bytes of the header and payload.
        /// </summary>
        public long TotalSizeInBytes { get; set; }

        /// <summary>
        /// Represents the text value of this object.
        /// </summary>
        public override string ToString()
        {
            return Encoding.UTF8.GetString(Payload);
        }

        public int CalculateHeaderSizeInBytes()
        {
            if (Payload.Length > UInt16.MaxValue)
                return 6;
            else if (Payload.Length > 125)
                return 4;
            else
                return 2;
        }

        public int CalculateMaskSizeInBytes()
        {
            if (Mask == null)
                return 0;
            else
                return Mask.Length;
        }

        public long CalculateTotalSizeInBytes()
        {
            if (Opcode == WsFrameType.Handshake)
                return Payload.Length;
            else if (Payload != null)
                return CalculateHeaderSizeInBytes() + CalculateMaskSizeInBytes() + Payload.Length;
            else
                return 2;
        }

        public string Print()
        {
            Encoders.WsTcpEncoder.SplitPayloadLength(PayloadLength, out byte length, out UInt16? extended1, out UInt64? extended2);

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
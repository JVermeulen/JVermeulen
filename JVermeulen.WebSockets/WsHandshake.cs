using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.WebSockets
{
  public static  class WsHandshake
    {
        private static readonly string ServerKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static readonly SHA1 CryptoServiceProvider = SHA1CryptoServiceProvider.Create();

        public static bool ValidateRequest(string content, out string response, out Guid? requestId)
        {
            response = null;
            requestId = null;

            try
            {
                var clientKeyAsString = content.Replace("ey:", "`").Split('`')[1].Replace("\r", "").Split('\n')[0].Trim();
                var clientKeyAsBytes = Convert.FromBase64String(clientKeyAsString);
                requestId = new Guid(clientKeyAsBytes);

                var acceptKey = AcceptKey(clientKeyAsString, ServerKey);
                
                response = AcceptResponse(acceptKey);
            }
            catch
            {
                //
            }

            return response != null;
        }

        private static string AcceptKey(string clientKey, string serverKey)
        {
            var data = Encoding.UTF8.GetBytes(clientKey + serverKey);
            var hash = CryptoServiceProvider.ComputeHash(data);

            return Convert.ToBase64String(hash);
        }

        private static string AcceptResponse(string acceptKey)
        {
            var builder = new StringBuilder();
            builder.AppendLine("HTTP/1.1 101 Switching Protocols");
            builder.AppendLine("Upgrade: websocket");
            builder.AppendLine("Connection: Upgrade");
            builder.AppendLine("Sec-WebSocket-Accept: " + acceptKey);
            builder.AppendLine();

            return builder.ToString();
        }
    }
}

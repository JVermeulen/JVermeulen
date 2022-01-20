using JVermeulen.Processing;
using JVermeulen.TCP.Core;
using JVermeulen.WebSockets;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var server = new WsServer(WsEncoder.TextEncoder, 8080);
                server.OptionBroadcastMessages = true;
                server.OptionEchoMessages = true;
                server.Start();

                Task.Delay(1000).Wait();

                var client = new WsClient(WsEncoder.TextEncoder, "127.0.0.1", 8080);
                client.Start();

                Task.Delay(1000).Wait();
                
                client.Stop();

                Console.WriteLine("Done!");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }
    }
}

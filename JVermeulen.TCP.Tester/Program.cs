using JVermeulen.WebSockets;
using System;
using System.Net;
using System.Net.WebSockets;
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
                ServerClientTest();

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

        private static void ServerClientTest()
        {
            using (var server = new WsServer2(WsEncoder.Text, "http://localhost:8082/"))
            {
                server.OptionBroadcastMessages = true;
                server.OptionEchoMessages = true;
                server.Start();

                Task.Delay(1000).Wait();

                using (var client = new WsClient2(WsEncoder.Text, "ws://localhost:8082/"))
                {
                    client.Start();

                    Task.Delay(1000).Wait();

                    client.Send("Hello World!");

                    Task.Delay(1000).Wait();
                }

                Task.Delay(60000).Wait();
            }
        }
    }
}

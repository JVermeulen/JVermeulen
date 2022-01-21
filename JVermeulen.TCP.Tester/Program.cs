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
                ClientServerTest();

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
            using (var server = new WsServer(WsEncoder.Text, "http://localhost:8082/"))
            {
                server.OptionLogToConsole = true;
                server.OptionBroadcastMessages = true;
                server.OptionEchoMessages = true;
                server.Start();

                Task.Delay(1000).Wait();

                using (var client = new WsClient(WsEncoder.Text, "ws://localhost:8082/"))
                {
                    client.OptionLogToConsole = true;
                    client.Start();

                    Task.Delay(1000).Wait();

                    client.Send("Hello World!");

                    Task.Delay(1000).Wait();
                }

                Task.Delay(60000).Wait();
            }
        }

        private static void ClientServerTest()
        {
            using (var client = new WsClient(WsEncoder.Text, "ws://localhost:8082/"))
            {
                client.OptionLogToConsole = true;

                Task.Delay(1000).Wait();

                using (var server = new WsServer(WsEncoder.Text, "http://localhost:8082/"))
                {
                    server.OptionLogToConsole = true;
                    server.OptionBroadcastMessages = true;
                    server.OptionEchoMessages = true;
                    server.Start();

                    Task.Delay(1000).Wait();
                    
                    client.Start();

                    Task.Delay(1000).Wait();

                    client.Send("1");

                    Task.Delay(1000).Wait();
                }

                Task.Delay(10000).Wait();

                client.Send("12");

                Task.Delay(10000).Wait();

                using (var server = new WsServer(WsEncoder.Text, "http://localhost:8082/"))
                {
                    server.OptionLogToConsole = true;
                    server.OptionBroadcastMessages = true;
                    server.OptionEchoMessages = true;
                    server.Start();

                    Task.Delay(1000).Wait();

                    client.Send("123");

                    Task.Delay(1000).Wait();
                }

                Task.Delay(5000).Wait();
            }
        }
    }
}

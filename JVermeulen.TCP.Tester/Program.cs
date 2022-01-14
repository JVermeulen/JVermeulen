using JVermeulen.Processing;
using JVermeulen.TCP.Core;
using JVermeulen.TCP.Encoders;
using JVermeulen.TCP.WebSocket;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Tester
{
    class Program
    {
        private static int counter = 0;

        static void Main(string[] args)
        {
            try
            {
                var server = new WsServer(WsTcpEncoder.Default, 8080);
                server.Start();

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

        private static void CreateClientAsync()
        {
            Task.Run(CreateClient).ConfigureAwait(false);
        }

        private static void CreateClient()
        {
            using (var connector = new TcpConnector(6000, "127.0.0.1"))
            {
                connector.ClientConnected += (s, e) => Console.WriteLine($"Client {e} connected.");
                connector.ClientConnected += (s, e) => e.Send(TestData());
                connector.ClientDisconnected += (s, e) => Console.WriteLine($"Client {e} disconnected.");
                connector.ExceptionOccured += (s, e) => Console.WriteLine($"Client error: {e}");
                connector.Start(false);

                Task.Delay(60000).Wait();
            }
        }

        private static byte[] TestData()
        {
            return new byte[] { 49, 00 };
            //var value = Interlocked.Increment(ref counter);

            //return Encoding.UTF8.GetBytes(value.ToString());
        }
    }
}

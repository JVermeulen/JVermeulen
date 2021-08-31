using JVermeulen.App;
using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Encoders;
using System;
using System.Buffers;
using System.Linq;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    class Program
    {
        private static int clientCount = 10;

        static void Main(string[] args)
        {
            var distributor = new ActorDistributor();

            using (var console = new ConsoleActor())
            {
                console.Start();
                distributor.Add(console);

                using (var server = new TcpServer<string>(StringTcpEncoder.NullByteUTF8Encoder, 6000))
                {
                    distributor.Add(server);

                    server.OptionEchoMessages = true;
                    server.OptionHeartbeatInterval = TimeSpan.FromSeconds(60);
                    server.Outbox.OptionWriteToConsole = false;
                    server.OptionBroadcastMessages = true;
                    server.OptionSendHeartbeatToOutbox = true;
                    server.SubscribeSafe<TcpSession<string>>(OnTcpSession, OnError);
                    server.Start();

                    Task.Delay(1000).Wait();

                    for (int i = 0; i < clientCount; i++)
                    {
                        StartClientAsync(i);
                    }

                    Task.Delay(10000).Wait();
                }
            }

            //TestAppInfo();
            //TestNetworkInfo();
        }

        private static void StartClientAsync(int index)
        {
            Task.Run(() => StartClient(index)).ConfigureAwait(false);
        }

        private static void StartClient(int index)
        {
            using (var client = new TcpClient<string>(StringTcpEncoder.NullByteUTF8Encoder, "127.0.0.1", 6000))
            {
                client.Start();

                Task.Delay(500).Wait();

                client.Send($"Client {index}");

                Task.Delay(500).Wait();
            }
        }

        private static void OnTcpSession(SessionMessage message)
        {
            if (message.Find<TcpSession<string>, SessionStatus>(out SessionMessage statusMessage))
            {
                //
            }

            if (message.Find<TcpSession<string>, ContentMessage<string>>(out SessionMessage tcpMessage))
            {
                //
            }
        }

        private static void OnError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }

        private static void OnCompleted()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Completed");
            Console.ResetColor();
        }

        private static void TestAppInfo()
        {
            Console.WriteLine("AppInfo:");
            Console.WriteLine($"- Name={AppInfo.Name}");
            Console.WriteLine($"- Title={AppInfo.Title}");
            Console.WriteLine($"- Version={AppInfo.Version}");
            Console.WriteLine($"- Architecture={AppInfo.Architecture}");
            Console.WriteLine($"- Description={AppInfo.Description}");
            Console.WriteLine($"- Guid={AppInfo.Guid}");
            Console.WriteLine($"- Is64bit={AppInfo.Is64bit}");
            Console.WriteLine($"- FileName={AppInfo.FileName}");
            Console.WriteLine($"- DirectoryName={AppInfo.DirectoryName}");
            Console.WriteLine($"- HasUI={AppInfo.HasUI}");
            Console.WriteLine($"- HasWindow={AppInfo.HasWindow}");
            Console.WriteLine($"- Type={AppInfo.Type}");
            Console.WriteLine($"- Culture={AppInfo.Culture}");
            Console.WriteLine($"- IsDebug={AppInfo.IsDebug}");
            Console.WriteLine($"- OSFriendlyName={AppInfo.OSFriendlyName}");
            Console.WriteLine($"- OSDescription={AppInfo.OSDescription}");
            Console.WriteLine();
        }

        public static void TestNetworkInfo()
        {

            Console.WriteLine("NetworkInfo:");
            Console.WriteLine($"- Primary (v4): {NetworkInfo.PrimaryHostname} ({NetworkInfo.PrimaryIPAddress})");
            Console.WriteLine($"- Primary (v6): {NetworkInfo.PrimaryHostname} ({NetworkInfo.PrimaryIPAddressV6})");

            var networkAddresses = NetworkInfo.NetworkAddresses;
            foreach (var networkAddress in networkAddresses)
            {
                Console.WriteLine($"- Address (v4): {networkAddress.Value} ({networkAddress.Key})");
            }

            var networkAddressesV6 = NetworkInfo.NetworkAddressesV6;
            foreach (var networkAddress in networkAddressesV6)
            {
                Console.WriteLine($"- Address (v6): {networkAddress.Value} ({networkAddress.Key})");
            }
            Console.WriteLine();
        }

        public static void OnExceptionOccured(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }

        public static void OnExceptionOccured2(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}

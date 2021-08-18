using JVermeulen.App;
using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Encoders;
using System;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var server = new ExampleTcpServer(6000))
            {
                //using (var client = new TcpClient<string>(XmlTcpEncoder.UTF8Encoder, "127.0.0.1", 6000))
                //{
                //    client.Start();

                //    Task.Delay(5000).Wait();
                //}

                Task.Delay(10000).Wait();
            }
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
            var networkAddresses = NetworkInfo.NetworkAddresses;

            Console.WriteLine("NetworkInfo:");
            foreach (var networkAddress in networkAddresses)
            {
                Console.WriteLine($"- Address: {networkAddress.Value} ({networkAddress.Key})");
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

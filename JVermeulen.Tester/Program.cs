using JVermeulen.App;
using JVermeulen.App.Windows;
using JVermeulen.MQTT;
using JVermeulen.Processing;
using JVermeulen.TCP;
using JVermeulen.TCP.Core;
using JVermeulen.TCP.Encoders;
using System;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    class Program
    {
        static void Main(string[] args)
        {

            if (NetworkInfo.TryGetHttpClientSslSupport(out string[] protocols))
            {

            }

            var manager = new EventLogManager();

            var log = manager.GetOrCreateEventLog(EventLogManager.Application, "Test");

            if (log != null)
            {
                log.EntryWritten += Log_EntryWritten;
                log.EnableRaisingEvents = true;

                Task.Delay(3000).Wait();

                log.WriteEntry("Test", System.Diagnostics.EventLogEntryType.Information);

            }

            Console.ReadLine();

            //using (var client = new MqttClient("PI04.home"))
            //{
            //    Task.Delay(120000).Wait();
            //}
            //using (var acceptor = new TcpConnector(6000))
            //{
            //    acceptor.ClientConnected += (s, e) => Console.WriteLine($"Client {e} connected.");
            //    acceptor.ClientConnected += (s, e) => e.OptionEchoReceivedData = true;
            //    acceptor.ClientDisconnected += (s, e) => Console.WriteLine($"Client {e} disconnected.");
            //    acceptor.StateChanged += (s, e) => Console.WriteLine($"Server started: {e}");
            //    acceptor.ExceptionOccured += (s, e) => Console.WriteLine($"Server error: {e}");
            //    acceptor.Start(true);

            //    Task.Delay(1200000).Wait();
            //}

            //using (var server = new TcpServer<string>(StringTcpEncoder.NullByteUTF8Encoder, 6000))
            //{
            //    server.Outbox.OptionWriteToConsole = true;
            //    server.SubscribeSafe<TcpSession<string>>(OnTcpSession, OnError);
            //    server.Start();

            //    Console.ReadKey();
            //}

            //TestAppInfo();
            //TestNetworkInfo();
        }

        private static void Log_EntryWritten(object sender, System.Diagnostics.EntryWrittenEventArgs e)
        {
            string log = (string)sender.GetType().GetProperty("Log").GetValue(sender, null);

            Console.WriteLine($"Entry written in {e.Entry.MachineName}.{log}.{e.Entry.Source}: {e.Entry.Message}");
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

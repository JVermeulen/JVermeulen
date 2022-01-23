using JVermeulen.App;
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
            Console.ResetColor();
            Console.Clear();

            try
            {
                //wss://demo.piesocket.com/v3/channel_1?api_key=oCdCMcMPQpbvNjUIzqtvF1d2X2okWpDQj4AwARJuAgtjhzKxVEjQU6IdCjwm&notify_self
                //var ipAddresses = NetworkInfo.GetTraceRoute("demo.piesocket.com");

                //foreach (var ipAddress in ipAddresses)
                //{
                //    Console.WriteLine(ipAddress);
                //}

                using (var client = new WsClient(WsEncoder.Text, true, "demo.piesocket.com", 443, "v3/channel_1?api_key=oCdCMcMPQpbvNjUIzqtvF1d2X2okWpDQj4AwARJuAgtjhzKxVEjQU6IdCjwm&notify_self"))
                {
                    client.OptionLogToConsole = true;

                    if (client.Dns(out string dns))
                        Console.WriteLine(dns);
                    if (client.Ping(out string ping))
                        Console.WriteLine(ping);

                    client.Start();

                    string message = null;

                    while (message != "exit")
                    {
                        message = Console.ReadLine();

                        client.Send(message);
                    }
                }


                //if (ReadArguments(args, out string type, out string hostname))
                //{
                //    if (type == "server")
                //        StartAsServer(hostname);
                //    else if (type == "client")
                //        StartAsClient(hostname);
                //}

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

        private static bool ReadArguments(string[] args, out string type, out string hostname)
        {
            try
            {
                type = null;
                hostname = null;

                string key = null;
                string value = null;

                foreach (var arg in args)
                {
                    if (arg.StartsWith("--"))
                    {
                        key = arg;
                    }
                    else
                    {
                        value = arg;

                        if (key.Equals("--type", StringComparison.OrdinalIgnoreCase))
                            type = value.ToLower();
                        if (key.Equals("--host", StringComparison.OrdinalIgnoreCase))
                            hostname = value.ToLower();
                    }
                }

                return type != null && hostname != null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to read arguments.", ex);
            }
        }

        private static void StartAsServer(string hostname)
        {
            using (var server = new WsServer(WsEncoder.Text, false, hostname, 8082))
            {
                server.OptionLogToConsole = true;
                server.OptionBroadcastMessages = true;
                server.OptionEchoMessages = true;
                server.Start();

                string message = null;

                while (message != "exit")
                {
                    message = Console.ReadLine();

                    server.Send(message);
                }
            }
        }

        private static void StartAsClient(string hostname)
        {
            using (var client = new WsClient(WsEncoder.Text, false, hostname, 8082))
            {
                client.OptionLogToConsole = true;
                client.Start();

                string message = null;

                while (message != "exit")
                {
                    message = Console.ReadLine();

                    client.Send(message);
                }
            }
        }
    }
}

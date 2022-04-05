using JVermeulen.Monitoring;
using JVermeulen.Processing;
using JVermeulen.WebSockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Tester
{
    class Program
    {
        private static List<WsClient> Clients;
        private static Actor Heartbeat;

        static void Main(string[] args)
        {
            Console.ResetColor();
            Console.Clear();

            try
            {
                if (ReadArguments(args, out string type, out string address))
                {
                    if (type == "server")
                        StartAsServer(address);
                    else if (type == "client")
                        StartAsClient(address, 10);
                    else if (type == "chat")
                        StartAsChatServer(address);
                }

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

        private static bool ReadArguments(string[] args, out string type, out string address)
        {
            try
            {
                type = null;
                address = null;

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
                        if (key.Equals("--address", StringComparison.OrdinalIgnoreCase))
                            address = value.ToLower();
                    }
                }

                return type != null && address != null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to read arguments.", ex);
            }
        }

        private static void StartAsServer(string address)
        {
            using (var server = new WsServer(WsEncoder.Text, address, true))
            {
                server.Monitoring = new WsClient(WsEncoder.Text, "http://grafana.local:3000/api/live/push/test", true);
                server.Monitoring.OptionRequestHeader = new Tuple<string, string>("Authorization", "Bearer ==");
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

        private static void StartAsChatServer(string address)
        {
            using (var server = new WsServer(WsEncoder.Text, address, true))
            {
                server.OptionLogToConsole = true;
                server.OptionBroadcastMessages = true;
                //server.OptionEchoMessages = true;
                server.Start();

                string message = null;

                while (message != "exit")
                {
                    message = Console.ReadLine();

                    server.Send(message);
                }
            }
        }

        private static void StartAsClient(string address)
        {
            using (var client = new WsClient(WsEncoder.Text, address, true))
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

        private static long ToUnixTime(DateTime t)
        {
            return ((DateTimeOffset)t.ToUniversalTime()).ToUnixTimeSeconds();
        }

        private static void StartAsClient(string address, int numberOfClients)
        {
            Clients = new List<WsClient>();
            Heartbeat = new Actor(TimeSpan.FromMilliseconds(1000));
            Heartbeat.OptionSendHeartbeatToOutbox = true;
            Heartbeat.Outbox.SubscribeSafe(OnHeartbeat);

            for (int i = 0; i < numberOfClients; i++)
            {
                var client = new WsClient(WsEncoder.Text, address, true);
                client.OptionLogToConsole = true;
            
                Clients.Add(client);
                
                client.Start();
            }

            Heartbeat.Start();

            string message = null;

            while (message != "exit")
            {
                message = Console.ReadLine();

                if (int.TryParse(message, out int interval))
                {
                    Heartbeat.OptionHeartbeatInterval = TimeSpan.FromMilliseconds(interval);

                    Heartbeat.Restart();
                }
            }

            Heartbeat.Dispose();
            Clients.ForEach(c => c.Dispose());
        }

        private static void OnHeartbeat(SessionMessage obj)
        {
            Clients.ForEach(c => c.Send("1234567890"));
        }
    }
}

using JVermeulen.TCP;
using JVermeulen.Processing;
using JVermeulen.TCP.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Tester
{
    public class ExampleTcpServer : IDisposable
    {
        public TcpServer<string> Server { get; private set; }

        public ExampleTcpServer(int port)
        {
            Server = new TcpServer<string>(XmlTcpEncoder.UTF8Encoder, port);
            Server.Queue.Subscribe(OnServerMessage);
            Server.MessageQueue.Subscribe(OnServerMessage);
            Server.Start();
        }

        private void OnServerMessage(SessionMessage message)
        {
            if (message.Value is SessionMessage innerMessage)
                OnServerMessage(innerMessage);
            else
                Console.WriteLine($"{message.Sender}: {message.Value}");
        }

        public void Dispose()
        {
            Server?.Dispose();
        }
    }
}

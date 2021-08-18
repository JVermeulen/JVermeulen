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

        //private void OnSessionEvent(SessionEventArgs e)
        //{
        //    Console.ForegroundColor = ConsoleColor.Yellow;

        //    if (e.Session is TcpServer<string> tcpServer)
        //        Console.WriteLine($"Server ({tcpServer.ServerAddress}) status changed to {e.Status}");
        //    else if (e.Session is TcpSession<string> tcpSession)
        //        Console.WriteLine($"Client ({tcpSession.RemoteAddress}) status changed to {e.Status}");

        //    Console.ResetColor();
        //}

        //private void OnMessageDequeue(MessageEventArgs<string> e)
        //{
        //    if (e.InnerMessageEventArgs != null)
        //        OnMessageDequeue(e.InnerMessageEventArgs);
        //    else if (e is MessageReceivedEventArgs<string> messageReceivedEventArgs)
        //        OnMessageReceivedEvent(messageReceivedEventArgs);
        //    else if (e is MessageSentEventArgs<string> messageSentEventArgs)
        //        OnMessageSentEvent(messageSentEventArgs);
        //}

        //private void OnMessageReceivedEvent(MessageReceivedEventArgs<string> e)
        //{
        //    Console.ForegroundColor = ConsoleColor.Blue;
        //    Console.WriteLine($"{e.CreatedAt:HH:mm:ss} Message ({e.Sender}) received: {e.Content}");
        //    Console.ResetColor();
        //}

        //private void OnMessageSentEvent(MessageSentEventArgs<string> e)
        //{
        //    Console.ForegroundColor = ConsoleColor.Red;
        //    Console.WriteLine($"{e.CreatedAt:HH:mm:ss} Message ({e.Sender}) sent: {e.Content}");
        //    Console.ResetColor();
        //}

        public void Dispose()
        {
            Server?.Dispose();
        }
    }
}

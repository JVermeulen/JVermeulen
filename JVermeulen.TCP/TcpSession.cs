using JVermeulen.Processing;
using JVermeulen.TCP.Core;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace JVermeulen.TCP
{
    public class TcpSession<T> : Actor
    {
        public TcpConnection Connection { get; set; }

        public ITcpEncoder<T> Encoder { get; private set; }
        public MessageBox<ContentMessage<T>> MessageBox { get; private set; }
        public bool OptionPollOnHeartbeat { get; set; } = true;

        public bool IsServer { get; private set; }
        public string LocalAddress { get; private set; }
        public string RemoteAddress { get; private set; }
        public bool IsConnected => Connection.Socket.Connected;

        private T ContentToken { get; set; }

        /// <summary>
        /// The number of bytes received.
        /// </summary>
        public long NumberOfBytesReceived => Interlocked.Read(ref _NumberOfBytesReceived);
        private long _NumberOfBytesReceived;

        /// <summary>
        /// The number of bytes sent.
        /// </summary>
        public long NumberOfBytesSent => Interlocked.Read(ref _NumberOfBytesSent);
        private long _NumberOfBytesSent;

        /// <summary>
        /// The number of messages received.
        /// </summary>
        public long NumberOfMessagesReceived => Interlocked.Read(ref _NumberOfMessagesReceived);
        private long _NumberOfMessagesReceived;

        /// <summary>
        /// The number of messages sent.
        /// </summary>
        public long NumberOfMessagesSent => Interlocked.Read(ref _NumberOfMessagesSent);
        private long _NumberOfMessagesSent;

        public TcpSession(SocketAsyncEventArgs e, ITcpEncoder<T> encoder) : base(TimeSpan.FromSeconds(60))
        {
            Encoder = encoder;
            MessageBox = new MessageBox<ContentMessage<T>>();

            Connection = new TcpConnection(e);
            Connection.DataReceived += OnReceived;
            Connection.DataSent += OnSent;
            Connection.ExceptionOccured += OnExceptionOccured;

            IsServer = e.AcceptSocket != null;
            LocalAddress = Connection.Socket.LocalEndPoint.ToString();
            RemoteAddress = Connection.Socket.RemoteEndPoint.ToString();
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (OptionPollOnHeartbeat)
                PollClient();
        }

        private void PollClient()
        {
            var isActive = Connection.TryPollClient();

            var message = new TcpPoll(isActive);
            Outbox.Add(new SessionMessage(this, message));

            if (!isActive)
                Restart();
        }

        public void Send(ContentMessage<T> message)
        {
            var data = Encoder.Encode(message.Content);

            var buffer = new TcpBuffer(data);

            Connection.Send(buffer);
        }

        private void OnSent(object sender, TcpBuffer buffer)
        {
            Interlocked.Add(ref _NumberOfBytesSent, buffer.Length);
            Interlocked.Increment(ref _NumberOfMessagesSent);

            var message = new ContentMessage<T>(LocalAddress, RemoteAddress, false, false, ContentToken, buffer.Length - Encoder.NettoDelimeterLength);
            
            MessageBox.Add(message);
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            Connection.Start();
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            Connection.Stop();
        }

        private void OnReceived(object sender, TcpBuffer buffer)
        {
            while (Encoder.TryFindContent(buffer.Data, out T content, out int numberOfBytes))
            {
                Interlocked.Add(ref _NumberOfBytesReceived, numberOfBytes);
                Interlocked.Increment(ref _NumberOfMessagesReceived);

                buffer.Remove(numberOfBytes);

                var message = new ContentMessage<T>(RemoteAddress, LocalAddress, true, false, content, numberOfBytes - Encoder.NettoDelimeterLength);
                MessageBox.Add(message);
            }
        }

        private void OnExceptionOccured(object sender, Exception ex)
        {
            var message = new SessionMessage(this, ex);

            Outbox.Add(message);
        }

        public override string ToString()
        {
            if (IsServer)
                return $"TCP Session ({LocalAddress} <= {RemoteAddress})";
            else
                return $"TCP Session ({LocalAddress} => {RemoteAddress})";
        }

        public override void Dispose()
        {
            base.Dispose();

            Connection.DataReceived -= OnReceived;
            Connection.DataSent -= OnSent;
            Connection.ExceptionOccured -= OnExceptionOccured;
        }
    }
}

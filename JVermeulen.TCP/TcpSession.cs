using JVermeulen.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.TCP
{
    public class TcpSession<T> : HeartbeatSession
    {
        private HeartbeatSession Heartbeat { get; set; }

        public ValueCounter NumberOfBytesReceived { get; set; }
        public ValueCounter NumberOfBytesSent { get; set; }
        public ValueCounter NumberOfMessagesReceived { get; set; }
        public ValueCounter NumberOfMessagesSent { get; set; }

        public ITcpEncoder<T> Encoder { get; private set; }
        public Socket Socket { get; private set; }
        public bool IsServer { get; private set; }
        public string LocalAddress { get; private set; }
        public string RemoteAddress { get; private set; }
        public bool IsConnected => Socket.Connected;

        private TcpBuffer ReceiveBuffer { get; set; }
        private SocketAsyncEventArgs ReceiveEventArgs { get; set; }
        private SocketAsyncEventArgs SendEventArgs { get; set; }

        public SubscriptionQueue<SessionMessage> MessageQueue { get; private set; }

        public bool OptionPollOnHeartbeat { get; set; } = true;

        public TcpSession(Socket socket, bool isServer, ITcpEncoder<T> encoder) : base(TimeSpan.FromSeconds(60))
        {
            Socket = socket;
            IsServer = isServer;
            Encoder = encoder;

            LocalAddress = Socket.LocalEndPoint.ToString();
            RemoteAddress = Socket.RemoteEndPoint.ToString();

            NumberOfBytesReceived = new ValueCounter();
            NumberOfBytesSent = new ValueCounter();
            NumberOfMessagesReceived = new ValueCounter();
            NumberOfMessagesSent = new ValueCounter();

            ReceiveBuffer = new TcpBuffer();

            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.Completed += OnAsyncCompleted;
            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.Completed += OnAsyncCompleted;

            MessageQueue = new SubscriptionQueue<SessionMessage>();
        }

        protected override void OnHeartbeat(long count)
        {
            base.OnHeartbeat(count);

            if (OptionPollOnHeartbeat)
                PollClient();
        }

        private void PollClient()
        {
            var isActive = TryPollClient();

            var message = new TcpPoll(isActive);
            Queue.Enqueue(new SessionMessage(this, message));

            if (!isActive)
                Restart();
        }

        public bool TryPollClient()
        {
            try
            {
                return Socket.Connected && !(Socket.Poll(1, SelectMode.SelectRead));
            }
            catch
            {
                return false;
            }
        }

        public void Write(T content)
        {
            try
            {
                if (IsConnected)
                {
                    var data = Encoder.Encode(content);

                    SendEventArgs.SetBuffer(data);
                    SendEventArgs.UserToken = content;

                    if (!Socket.SendAsync(SendEventArgs))
                        OnSend(SendEventArgs);
                }
            }
            catch
            {
                Stop();
            }
        }

        private void OnSend(SocketAsyncEventArgs e)
        {
            NumberOfBytesSent.Add(e.BytesTransferred);
            NumberOfMessagesSent.Increment();

            var message = new TcpMessage<T>(LocalAddress, RemoteAddress, false, (T)e.UserToken, e.BytesTransferred);
            MessageQueue.Enqueue(new(this, message));
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            WaitForReceive();

            Heartbeat?.Start();
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            if (Socket.Connected)
            {
                Heartbeat?.Stop();

                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }
        }

        private void WaitForReceive()
        {
            if (Socket.Connected)
            {
                ReceiveEventArgs.SetBuffer(ReceiveBuffer.Buffer);

                if (!Socket.ReceiveAsync(ReceiveEventArgs))
                    OnReceived(ReceiveEventArgs);
            }
        }

        private void OnReceived(SocketAsyncEventArgs e)
        {
            try
            {
                if (Status == SessionStatus.Started)
                {
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        ReceiveBuffer.AddBufferToData(e.BytesTransferred);

                        while (ReceiveBuffer.Data.Length > 0 && Encoder.TryFindContent(ReceiveBuffer.Data, out T content, out byte[] nextContent))
                        {
                            NumberOfBytesReceived.Add(ReceiveBuffer.Data.Length - nextContent.Length);
                            NumberOfMessagesReceived.Increment();

                            var message = new TcpMessage<T>(RemoteAddress, LocalAddress, true, content, e.BytesTransferred);
                            MessageQueue.Enqueue(new SessionMessage(this, message));

                            ReceiveBuffer.Data = nextContent;
                        }

                        WaitForReceive();
                    }
                    else // Client disconnected
                    {
                        Stop();
                    }
                }
            }
            catch
            {
                Stop();
            }
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (Status == SessionStatus.Started)
            {
                if (e.LastOperation == SocketAsyncOperation.Receive)
                    OnReceived(e);
            }
        }

        public override string ToString()
        {
            if (IsServer)
                return $"TCP Session ({LocalAddress} <= {RemoteAddress})";
            else
                return $"TCP Session ({LocalAddress} => {RemoteAddress})";
        }
    }
}

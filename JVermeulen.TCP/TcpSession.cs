using JVermeulen.Processing;
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

        public ITcpEncoder<T> Encoder { get; private set; }
        public Socket Socket { get; private set; }
        public bool IsServer { get; private set; }
        public string LocalAddress { get; private set; }
        public string RemoteAddress { get; private set; }
        public bool IsConnected => Socket.Connected;

        private TcpBuffer ReceiveBuffer { get; set; }
        private TcpBuffer SendBuffer { get; set; }

        private SocketAsyncEventArgs ReceiveEventArgs { get; set; }
        private SocketAsyncEventArgs SendEventArgs { get; set; }

        public MessageBox<SessionMessage> MessageBox { get; private set; }

        public bool OptionPollOnHeartbeat { get; set; } = true;

        public TcpSession(Socket socket, bool isServer, ITcpEncoder<T> encoder) : base(TimeSpan.FromSeconds(60))
        {
            Socket = socket;
            IsServer = isServer;
            Encoder = encoder;

            LocalAddress = Socket.LocalEndPoint.ToString();
            RemoteAddress = Socket.RemoteEndPoint.ToString();

            ReceiveBuffer = new TcpBuffer();
            SendBuffer = new TcpBuffer();

            var receiveBuffer = ArrayPool<byte>.Shared.Rent(1024);
            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            ReceiveEventArgs.Completed += OnAsyncCompleted;

            var sendBuffer = ArrayPool<byte>.Shared.Rent(1024);
            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            SendEventArgs.Completed += OnAsyncCompleted;

            MessageBox = new MessageBox<SessionMessage>();
        }

        protected override void OnHeartbeat(Heartbeat heartbeat)
        {
            base.OnHeartbeat(heartbeat);

            if (OptionPollOnHeartbeat)
                PollClient();
        }

        private void PollClient()
        {
            var isActive = TryPollClient();

            var message = new TcpPoll(isActive);
            Outbox.Add(new SessionMessage(this, message));

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

        public void Send(ContentMessage<T> message)
        {
            try
            {
                message.IsIncoming = false;
                message.IsRequest = false;
                message.SenderAddress = LocalAddress;
                message.DestinationAddress = RemoteAddress;

                if (IsConnected)
                {
                    var data = Encoder.Encode((T)message.Content);

                    SendEventArgs.SetBuffer(data);
                    SendEventArgs.UserToken = message;

                    if (!Socket.SendAsync(SendEventArgs))
                        OnSent(SendEventArgs);
                }
            }
            catch (Exception ex)
            {
                OnExceptionOccured(ex);
            }
        }

        private void OnSent(SocketAsyncEventArgs e)
        {
            int bytesSent = e.BytesTransferred;

            Interlocked.Add(ref _NumberOfBytesSent, bytesSent);
            Interlocked.Increment(ref _NumberOfMessagesSent);

            var message = (ContentMessage<T>)e.UserToken;
            message.ContentInBytes = e.BytesTransferred;

            MessageBox.Add(new(this, message));
        }

        protected override void OnStarting()
        {
            base.OnStarting();

            WaitForReceive();
        }

        protected override void OnStopping()
        {
            base.OnStopping();

            if (Socket.Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }
        }

        private void WaitForReceive()
        {
            if (Socket.Connected)
            {
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
                        ReceiveBuffer.Add(e.Buffer, e.Offset, e.BytesTransferred);

                        while (Encoder.TryFindContent(ReceiveBuffer, out T content, out int numberOfBytes))
                        {
                            ReceiveBuffer.Remove(numberOfBytes);

                            Interlocked.Add(ref _NumberOfBytesReceived, numberOfBytes);
                            Interlocked.Increment(ref _NumberOfMessagesReceived);

                            var message = new ContentMessage<T>(RemoteAddress, LocalAddress, true, false, content, numberOfBytes - Encoder.DelimeterNettoLength);
                            MessageBox.Add(new SessionMessage(this, message));
                        }

                        WaitForReceive();
                    }
                    else // Client disconnected
                    {
                        Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                OnExceptionOccured(ex);
            }
        }

        private void OnExceptionOccured(Exception ex)
        {
            TcpException exception;
            
            if (ex is SocketException socketException)
                exception = new TcpException("Unable to continue TCP session because of an socket error.", ex, socketException.SocketErrorCode);
            else
                exception = new TcpException("Unable to contintue TCP session.", ex);

            var message = new SessionMessage(this, exception);

            Outbox.Add(message);

            Stop();
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

        public override void Dispose()
        {
            base.Dispose();

            ArrayPool<byte>.Shared.Return(ReceiveEventArgs.Buffer);
            ArrayPool<byte>.Shared.Return(SendEventArgs.Buffer);
        }
    }
}

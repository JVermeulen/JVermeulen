using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;

namespace JVermeulen.TCP
{
    public class TcpConnection : IDisposable
    {
        /// <summary>
        /// The default size of the receive and send buffer created. Default is 1024.
        /// </summary>
        public int DefaultMinimumBufferLength { get; set; } = 1024;

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

        public Socket Socket { get; private set; }
        public bool IsConnected => Socket.Connected;
        public bool IsServer { get; private set; }
        public string LocalAddress { get; private set; }
        public string RemoteAddress { get; private set; }
        private SocketAsyncEventArgs ReceiveEventArgs { get; set; }
        private SocketAsyncEventArgs SendEventArgs { get; set; }

        public EventHandler<TcpBuffer> DataReceived { get; set; }
        public EventHandler<TcpBuffer> DataSent { get; set; }
        public EventHandler<Exception> OnExceptionOccured { get; set; }

        public TcpConnection(SocketAsyncEventArgs e)
        {
            Socket = e.AcceptSocket ?? e.ConnectSocket;
            IsServer = e.AcceptSocket != null;

            LocalAddress = Socket.LocalEndPoint.ToString();
            RemoteAddress = Socket.RemoteEndPoint.ToString();

            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.Completed += OnAsyncCompleted;

            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.Completed += OnAsyncCompleted;
        }

        public void Start()
        {
            if (!Socket.Connected)
            {
                var receiveBuffer = ArrayPool<byte>.Shared.Rent(DefaultMinimumBufferLength);
                ReceiveEventArgs.SetBuffer(receiveBuffer, 0, DefaultMinimumBufferLength);

                var sendBuffer = ArrayPool<byte>.Shared.Rent(DefaultMinimumBufferLength);
                SendEventArgs.SetBuffer(sendBuffer, 0, DefaultMinimumBufferLength);

                WaitForReceive();
            }
        }

        public void Stop()
        {
            if (Socket.Connected)
            {
                if (ReceiveEventArgs.Buffer != null)
                    ArrayPool<byte>.Shared.Return(ReceiveEventArgs.Buffer);

                if (SendEventArgs.Buffer != null)
                    ArrayPool<byte>.Shared.Return(SendEventArgs.Buffer);

                if (Socket.Connected)
                {
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Close();
                }
            }
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
                OnReceived(e);
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
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    Interlocked.Add(ref _NumberOfBytesReceived, e.BytesTransferred);
                    Interlocked.Increment(ref _NumberOfMessagesReceived);

                    var buffer = new TcpBuffer(e.Buffer, e.Offset, e.BytesTransferred);
                    DataSent?.Invoke(this, buffer);

                    WaitForReceive();
                }
                else // Client disconnected
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        private void OnException(Exception ex)
        {
            TcpException exception;

            if (ex is SocketException socketException)
                exception = new TcpException("Unable to continue TCP session because of an socket error.", ex, socketException.SocketErrorCode);
            else
                exception = new TcpException("Unable to contintue TCP session.", ex);

            OnExceptionOccured?.Invoke(this, exception);

            Stop();
        }

        public void Send(TcpBuffer buffer)
        {
            if (IsConnected)
            {
                SendEventArgs.SetBuffer(buffer.Data);
                SendEventArgs.UserToken = buffer;

                if (!Socket.SendAsync(SendEventArgs))
                    OnSent(SendEventArgs);
            }
        }

        private void OnSent(SocketAsyncEventArgs e)
        {
            int bytesSent = e.BytesTransferred;
            var buffer = (TcpBuffer)e.UserToken;

            Interlocked.Add(ref _NumberOfBytesSent, bytesSent);
            Interlocked.Increment(ref _NumberOfMessagesSent);

            DataSent?.Invoke(this, buffer);
        }

        public override string ToString()
        {
            if (IsServer)
                return $"TCP Session ({LocalAddress} <= {RemoteAddress})";
            else
                return $"TCP Session ({LocalAddress} => {RemoteAddress})";
        }

        public void Dispose()
        {
            ReceiveEventArgs.Completed -= OnAsyncCompleted;
            SendEventArgs.Completed -= OnAsyncCompleted;
        }
    }
}

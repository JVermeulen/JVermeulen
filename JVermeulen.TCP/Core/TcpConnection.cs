using System;
using System.Buffers;
using System.Net.Sockets;

namespace JVermeulen.TCP.Core
{
    public class TcpConnection : IDisposable
    {
        /// <summary>
        /// The default size of the receive and send buffer created. Default is 1024.
        /// </summary>
        public int DefaultMinimumBufferLength { get; set; } = 1024;

        public Socket Socket { get; private set; }
        public bool IsStarted { get; private set; }
        private SocketAsyncEventArgs ReceiveEventArgs { get; set; }
        private SocketAsyncEventArgs SendEventArgs { get; set; }
        private TcpBuffer ReceiveBuffer { get; set; }

        public EventHandler<TcpBuffer> DataReceived { get; set; }
        public EventHandler<TcpBuffer> DataSent { get; set; }
        public EventHandler<Exception> ExceptionOccured { get; set; }

        public TcpConnection(SocketAsyncEventArgs e)
        {
            Socket = e.AcceptSocket ?? e.ConnectSocket;

            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.Completed += OnAsyncCompleted;

            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.Completed += OnAsyncCompleted;

            ReceiveBuffer = new TcpBuffer();
        }

        public void Start()
        {
            if (!IsStarted)
            {
                var receiveBuffer = ArrayPool<byte>.Shared.Rent(DefaultMinimumBufferLength);
                ReceiveEventArgs.SetBuffer(receiveBuffer, 0, DefaultMinimumBufferLength);

                var sendBuffer = ArrayPool<byte>.Shared.Rent(DefaultMinimumBufferLength);
                SendEventArgs.SetBuffer(sendBuffer, 0, DefaultMinimumBufferLength);

                WaitForReceive();

                IsStarted = true;
            }
        }

        public void Stop()
        {
            if (IsStarted)
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

                IsStarted = false;
            }
        }

        private void OnExceptionOccured(Exception ex)
        {
            TcpException exception;

            if (ex is SocketException socketException)
                exception = new TcpException("Unable to continue TCP session because of an socket error.", ex, socketException.SocketErrorCode);
            else
                exception = new TcpException("Unable to contintue TCP session.", ex);

            ExceptionOccured?.Invoke(this, exception);

            Stop();
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
                    ReceiveBuffer.Add(e.Buffer, e.Offset, e.BytesTransferred);

                    DataReceived?.Invoke(this, ReceiveBuffer);

                    WaitForReceive();
                }
                else // Client disconnected
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                OnExceptionOccured(ex);
            }
        }

        public void Send(TcpBuffer buffer)
        {
            if (Socket.Connected)
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

            DataSent?.Invoke(this, buffer);
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

        public void Dispose()
        {
            Stop();

            ReceiveEventArgs.Completed -= OnAsyncCompleted;
            SendEventArgs.Completed -= OnAsyncCompleted;
        }
    }
}

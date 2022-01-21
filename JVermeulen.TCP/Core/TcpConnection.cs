using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.TCP.Core
{
    /// <summary>
    /// A TCP connection between client and server.
    /// </summary>
    public class TcpConnection : IDisposable, IEquatable<TcpConnection>
    {
        /// <summary>
        /// A global unique Id.
        /// </summary>
        private static long GlobalId;

        /// <summary>
        /// A unique Id for this session.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The default size of the receive and send buffer created. Default is 1024.
        /// </summary>
        public int DefaultMinimumBufferLength { get; set; } = 1024;

        /// <summary>
        /// The internal socket (client or server).
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        /// When true, this connection is running as server.
        /// </summary>
        public bool IsServer { get; private set; }

        /// <summary>
        /// When true, this connection is sending and receiving data.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// The reusable SocketAsyncEventArgs for receiving data.
        /// </summary>
        private SocketAsyncEventArgs ReceiveEventArgs { get; set; }

        /// <summary>
        /// The reusable SocketAsyncEventArgs for sender data.
        /// </summary>
        private SocketAsyncEventArgs SendEventArgs { get; set; }

        /// <summary>
        /// Internal buffer for receiving data.
        /// </summary>
        private TcpBuffer ReceiveBuffer { get; set; }

        /// <summary>
        /// Raised when data has been received. The buffer could be message incomplete or has a part of the next message.
        /// </summary>
        public EventHandler<TcpBuffer> DataReceived { get; set; }

        /// <summary>
        /// Raised when data has been sent.
        /// </summary>
        public EventHandler<TcpBuffer> DataSent { get; set; }

        /// <summary>
        /// Raised when an socket exception occured.
        /// </summary>
        public EventHandler<Exception> ExceptionOccured { get; set; }

        /// <summary>
        /// Raised when this object is started (true) or stopped (false).
        /// </summary>
        public EventHandler<bool> StateChanged { get; set; }

        /// <summary>
        /// The local address.
        /// </summary>
        public string LocalAddress { get; private set; }

        /// <summary>
        /// The remote address.
        /// </summary>
        public string RemoteAddress { get; private set; }

        /// <summary>
        /// When true, incoming data is directly send back. Default is false.
        /// </summary>
        public bool OptionEchoReceivedData { get; set; } = false;

        /// <summary>
        /// When true, incoming data is send to the DataReceived event. Default is true.
        /// </summary>
        public bool OptionRaiseReceivedData { get; set; } = true;

        /// <summary>
        /// When true, sent data is send to the DataSent event. Default is false.
        /// </summary>
        public bool OptionRaiseSentData { get; set; } = false;

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="e">The SocketAsyncEventArgs from the server or client.</param>
        public TcpConnection(SocketAsyncEventArgs e)
        {
            Id = Interlocked.Increment(ref GlobalId);

            Socket = e.AcceptSocket ?? e.ConnectSocket;
            IsServer = e.AcceptSocket != null;
            LocalAddress = Socket.LocalEndPoint.ToString();
            RemoteAddress = Socket.RemoteEndPoint.ToString();

            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.Completed += OnAsyncCompleted;

            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.Completed += OnAsyncCompleted;

            ReceiveBuffer = new TcpBuffer();
        }

        /// <summary>
        /// Start waiting for incoming data.
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                var receiveBuffer = ArrayPool<byte>.Shared.Rent(DefaultMinimumBufferLength);
                ReceiveEventArgs.SetBuffer(receiveBuffer, 0, DefaultMinimumBufferLength);

                var sendBuffer = ArrayPool<byte>.Shared.Rent(DefaultMinimumBufferLength);
                SendEventArgs.SetBuffer(sendBuffer, 0, DefaultMinimumBufferLength);

                IsStarted = true;

                StateChanged?.Invoke(this, true);

                WaitForReceive();
            }
        }

        /// <summary>
        /// Stop waiting for incoming data.
        /// </summary>
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

                StateChanged?.Invoke(this, false);
                IsStarted = false;
            }
        }

        /// <summary>
        /// Handles exceptions.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        private void OnExceptionOccured(Exception ex)
        {
            TcpException exception;

            if (ex is SocketException socketException)
                exception = new TcpException("Unable to continue TCP connection because of an socket error.", ex, socketException.SocketErrorCode);
            else
                exception = new TcpException("Unable to contintue TCP connection.", ex);

            ExceptionOccured?.Invoke(this, exception);

            Stop();
        }

        /// <summary>
        /// Handles send/received is done.
        /// </summary>
        /// <param name="sender">Always this object.</param>
        /// <param name="e">The SocketAsyncEventArgs of the socket.</param>
        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
                OnReceived(e);
        }

        /// <summary>
        /// Wait for incoming data.
        /// </summary>
        private void WaitForReceive()
        {
            if (Socket.Connected)
            {
                if (!Socket.ReceiveAsync(ReceiveEventArgs))
                    OnReceived(ReceiveEventArgs);
            }
        }

        /// <summary>
        /// Handle incoming data.
        /// </summary>
        /// <param name="e">The SocketAsyncEventArgs of the socket.</param>
        private void OnReceived(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    if (OptionEchoReceivedData)
                    {
                        var data = e.MemoryBuffer.Slice(e.Offset, e.BytesTransferred);

                        Send(data);
                    }

                    if (OptionRaiseReceivedData)
                    {
                        ReceiveBuffer.Add(e.Buffer, e.Offset, e.BytesTransferred);

                        DataReceived?.Invoke(this, ReceiveBuffer);
                    }

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

        /// <summary>
        /// Send the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to send.</param>
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

        /// <summary>
        /// Send the given data. The DataSent event will not be raised because TcpBuffer is not available.
        /// </summary>
        /// <param name="data">The data to send.</param>
        public void Send(Memory<byte> data)
        {
            if (Socket.Connected)
            {
                SendEventArgs.SetBuffer(data);

                if (!Socket.SendAsync(SendEventArgs))
                    OnSent(SendEventArgs);
            }
        }

        /// <summary>
        /// Handles data that has been sent.
        /// </summary>
        /// <param name="e">The SocketAsyncEventArgs of the socket.</param>
        private void OnSent(SocketAsyncEventArgs e)
        {
            int bytesSent = e.BytesTransferred;

            if (OptionRaiseSentData && e.UserToken != null)
            {
                var buffer = (TcpBuffer)e.UserToken;

                DataSent?.Invoke(this, buffer);
            }
        }

        /// <summary>
        /// Poll the connection.
        /// </summary>
        public bool TryPollConnection()
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

        /// <summary>
        /// Returns true when this object equals the given object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        public override bool Equals(object obj)
        {
            return Equals(obj as TcpConnection);
        }

        /// <summary>
        /// Returns true when this object equals the given object.
        /// </summary>
        /// <param name="other">The object to compare to.</param>
        public bool Equals(TcpConnection other)
        {
            return Id == other.Id;
        }

        /// <summary>
        /// Returns the hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// The string value of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsServer)
                return $"TCP connection ({LocalAddress} <= {RemoteAddress})";
            else
                return $"TCP connection ({LocalAddress} => {RemoteAddress})";
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Stop();

            ReceiveEventArgs.Completed -= OnAsyncCompleted;
            SendEventArgs.Completed -= OnAsyncCompleted;
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;

namespace JVermeulen.TCP.Core
{
    /// <summary>
    /// Creates a TCP socket using SocketAsyncEventArgs.
    /// </summary>
    public class TcpConnector : IDisposable
    {
        /// <summary>
        /// When true, this connector is running.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// When true, is connector starts waiting for client to connect. Otherwise, connect to a server.
        /// </summary>
        public bool IsServer { get; private set; }

        /// <summary>
        /// The internal TCP Socket.
        /// </summary>
        protected Socket Socket { get; set; }

        /// <summary>
        /// The remote or local endpoint.
        /// </summary>
        protected IPEndPoint ServerEndPoint { get; set; }

        /// <summary>
        /// The remote or local endpoint.
        /// </summary>
        protected IPEndPoint ClientEndPoint { get; set; }

        /// <summary>
        /// The reusable SocketAsyncEventArgs.
        /// </summary>
        private SocketAsyncEventArgs ConnectorEventArgs { get; set; }

        /// <summary>
        /// Raised when an exception occured.
        /// </summary>
        public EventHandler<Exception> ExceptionOccured { get; set; }

        /// <summary>
        /// Raised when this object is started (true) or stopped (false).
        /// </summary>
        public EventHandler<bool> StateChanged { get; set; }

        /// <summary>
        /// Raised when a connection is started..
        /// </summary>
        public EventHandler<TcpConnection> ClientConnected { get; set; }

        /// <summary>
        /// Raised when a connection is stopped.
        /// </summary>
        public EventHandler<TcpConnection> ClientDisconnected { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="port">The port number of the server.</param>
        /// <param name="address">The address of the server.</param>
        public TcpConnector(int port, string address = null) : this(new IPEndPoint(address == null ? IPAddress.Any : IPAddress.Parse(address), port))
        {
            //
        }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="serverEndpoint">The endpoint to use.</param>
        public TcpConnector(IPEndPoint serverEndpoint)
        {
            ServerEndPoint = serverEndpoint;

            ConnectorEventArgs = new SocketAsyncEventArgs();
            ConnectorEventArgs.Completed += OnConnected;
        }

        /// <summary>
        /// Start accepting client connections or connect to a server.
        /// </summary>
        /// <param name="isServer">When true, is connector starts waiting for client to connect. Otherwise, connect to a server.</param>
        public void Start(bool isServer)
        {
            if (!IsStarted)
            {
                IsServer = isServer;

                Socket = new Socket(ServerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (isServer)
                    StartAsServer();
                else
                    StartAsClient();

                IsStarted = true;

                StateChanged?.Invoke(this, true);
            }
        }

        /// <summary>
        /// Start accepting clients.
        /// </summary>
        private void StartAsServer()
        {
            if (ServerEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, ServerEndPoint.Port));
            }
            else
            {
                Socket.Bind(ServerEndPoint);
            }

            ServerEndPoint = (IPEndPoint)Socket.LocalEndPoint;
            Socket.Listen();

            Accept(ConnectorEventArgs);
        }

        /// <summary>
        /// Start connecting to a server.
        /// </summary>
        private void StartAsClient()
        {
            ConnectorEventArgs.RemoteEndPoint = ServerEndPoint;

            if (!Socket.ConnectAsync(ConnectorEventArgs))
                OnConnected(this, ConnectorEventArgs);
        }

        /// <summary>
        /// Stop accepting client connections.
        /// </summary>
        public void Stop()
        {
            if (IsStarted)
            {
                Socket?.Dispose();

                IsStarted = false;

                StateChanged?.Invoke(this, false);
            }
        }

        /// <summary>
        /// Starting accepting client connections on the socket.
        /// </summary>
        /// <param name="e"></param>
        private void Accept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;

            if (!Socket.AcceptAsync(e))
                OnConnected(this, e);
        }

        /// <summary>
        /// Handle a client connection.
        /// </summary>
        /// <param name="sender">Always this object.</param>
        /// <param name="e">The SocketAsyncEventArgs.</param>
        private void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var connection = new TcpConnection(e);
                connection.OptionEchoReceivedData = true;
                connection.StateChanged += OnConnectionStateChanged;
                connection.Start();

                if (IsServer)
                    Accept(e);
            }
            else
            {
                var ex = new TcpException($"Unable to connect to server '{ServerEndPoint}'.", null, e.SocketError);

                ExceptionOccured?.Invoke(this, ex);

                Stop();
            }
        }

        /// <summary>
        /// When a connection started/stopped.
        /// </summary>
        /// <param name="sender">The TcpConnection.</param>
        /// <param name="isStarted">True when started.</param>
        private void OnConnectionStateChanged(object sender, bool isStarted)
        {
            var connection = (TcpConnection)sender;

            if (isStarted)
            {
                ClientConnected?.Invoke(this, connection);
            }
            else
            {
                ClientDisconnected?.Invoke(this, connection);

                connection.StateChanged -= OnConnectionStateChanged;
            }
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Stop();

            ConnectorEventArgs.Completed -= OnConnected;
        }
    }
}

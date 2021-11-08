using System;
using System.Net.Sockets;

namespace JVermeulen.TCP.Core
{
    /// <summary>
    /// TCP Exception.
    /// </summary>
    public class TcpException : Exception
    {
        /// <summary>
        /// When the inner exception is a SocketException (optional).
        /// </summary>
        public SocketError? SocketError { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="error">The inner SocketError.</param>
        public TcpException(string message, Exception innerException = null, SocketError? error = null) : base(message, innerException)
        {
            SocketError = error;
        }
    }
}

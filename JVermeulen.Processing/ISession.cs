using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// An interface for the session pattern.
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Start the session.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the session.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stop and start the session.
        /// </summary>
        void Restart();
    }
}

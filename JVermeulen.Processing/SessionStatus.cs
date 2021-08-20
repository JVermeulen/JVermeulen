using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// The status of the session.
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// The session is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The session is started.
        /// </summary>
        Started,

        /// <summary>
        /// The session is stopping.
        /// </summary>
        Stopping,

        /// <summary>
        /// The session is starting.
        /// </summary>
        Starting,
    }
}

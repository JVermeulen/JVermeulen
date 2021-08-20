using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Represents a heartbeat.
    /// </summary>
    public class Heartbeat
    {
        /// <summary>
        /// The incremented number of this heartbeat, starting with 0.
        /// </summary>
        public long Count { get; private set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="count">The incremented number of this heartbeat, starting with 0.</param>
        public Heartbeat(long count)
        {
            Count = count;
        }

        /// <summary>
        /// A String that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"Heartbeat ({Count})";
        }
    }
}

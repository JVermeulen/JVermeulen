using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A generic session message.
    /// </summary>
    public class SessionMessage
    {
        /// <summary>
        /// A unique Id for this message.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The time this message has been created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// The sender of this message.
        /// </summary>
        public Session Sender { get; private set; }

        /// <summary>
        /// The value of this message.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="sender">The sender of this message.</param>
        /// <param name="value">The value of this message.</param>
        public SessionMessage(Session sender, object value)
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;

            Sender = sender;
            Value = value;
        }

        /// <summary>
        /// A String that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"{Sender}: {Value}";
        }
    }
}

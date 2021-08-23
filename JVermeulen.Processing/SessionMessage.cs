using System;
using System.Collections.Generic;

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
        /// Checks if the Value is of the given type.
        /// </summary>
        /// <param name="types">The types to check for.</param>
        /// <param name="recursive">When true, checks for the inner SessionMessage.</param>
        /// <returns></returns>
        public bool ValueIsTypeof(List<Type> types, int recursive = 0)
        {
            if (Value == null)
                return false;
            else if (Value is SessionMessage sessionMessage && recursive > 0)
                return sessionMessage.ValueIsTypeof(types, recursive - 1);
            else
                return types.Contains(Value.GetType());
        }

        /// <summary>
        /// Checks if the Value is of the given type. T is the type to check for.
        /// </summary>
        /// <param name="recursive">When true, checks for the inner SessionMessage.</param>
        public bool ValueIsTypeof<T>(int recursive = 0)
        {
            var types = new List<Type>() { typeof(T) };

            return ValueIsTypeof(types, recursive);
        }

        /// <summary>
        /// Checks if the Value is of the given type. Tx is the types to check for.
        /// </summary>
        /// <param name="recursive">When true, checks for the inner SessionMessage.</param>
        public bool ValueIsTypeof<T1, T2>(int recursive = 0)
        {
            var types = new List<Type>() { typeof(T1), typeof(T2) };

            return ValueIsTypeof(types, recursive);
        }

        /// <summary>
        /// Checks if the Value is of the given type. Tx is the types to check for.
        /// </summary>
        /// <param name="recursive">When true, checks for the inner SessionMessage.</param>
        public bool ValueIsTypeof<T1, T2, T3>(int recursive = 0)
        {
            var types = new List<Type>() { typeof(T1), typeof(T2), typeof(T3) };

            return ValueIsTypeof(types, recursive);
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

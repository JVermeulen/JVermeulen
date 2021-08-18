using System;

namespace JVermeulen.Processing
{
    public class SessionMessage
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Session Sender { get; private set; }
        public object Value { get; private set; }

        public SessionMessage(Session sender, object value)
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;

            Sender = sender;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Sender}: {Value}";
        }
    }
}

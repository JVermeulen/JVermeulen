using System;

namespace JVermeulen.TCP
{
    public class TcpMessage<T>
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public string Sender { get; private set; }
        public string Destination { get; private set; }
        public bool IsIncoming { get; private set; }
        public T Content { get; private set; }
        public int ContentInBytes { get; private set; }

        public TcpMessage(string sender, string destination, bool isIncoming, T content, int contentInBytes)
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;

            Sender = sender;
            Destination = destination;
            IsIncoming = isIncoming;
            Content = content;
            ContentInBytes = contentInBytes;
        }

        public override string ToString()
        {
            var direction = IsIncoming ? "received" : "sent";

            return $"TCP Message {direction} ({Content})";
        }
    }
}

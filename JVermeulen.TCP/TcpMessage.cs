using System;

namespace JVermeulen.TCP
{
    public class TcpMessage<T>
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public string Sender { get; set; }
        public string Destination { get; set; }
        public bool IsIncoming { get; set; }
        public T Content { get; set; }
        public int? ContentInBytes { get; set; }

        public TcpMessage(string sender, string destination, bool isIncoming, T content, int? contentInBytes = null)
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

            return $"TCP Message {direction} ({ContentInBytes} bytes)";
        }
    }
}

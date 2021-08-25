﻿using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A generic content message.
    /// </summary>
    public class ContentMessage<T>
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
        /// The content of this message.
        /// </summary>
        public T Content { get; set; }

        /// <summary>
        /// The type of content.
        /// </summary>
        public Type ContentType => GetType().GetGenericArguments()[0];

        /// <summary>
        /// The address of the sender.
        /// </summary>
        public string SenderAddress { get; set; }

        /// <summary>
        /// The address of the destination.
        /// </summary>
        public string DestinationAddress { get; set; }

        /// <summary>
        /// When true, this message has been received.
        /// </summary>
        public bool IsIncoming { get; set; }

        /// <summary>
        /// When true, this message is a request to be send.
        /// </summary>
        public bool IsRequest { get; set; }

        /// <summary>
        /// The size of the content in bytes (optional).
        /// </summary>
        public int? ContentInBytes { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        public ContentMessage(string senderAddress, string destinationAddress, bool isIncoming, T content, int? contentInBytes = null)
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;

            SenderAddress = senderAddress;
            DestinationAddress = destinationAddress;
            IsIncoming = isIncoming;
            Content = content;
            ContentInBytes = contentInBytes;
        }

        /// <summary>
        /// A String that represents the current object.
        /// </summary>
        public override string ToString()
        {
            var direction = IsIncoming ? "received" : "sent";
            var size = ContentInBytes.HasValue ? $" ({ContentInBytes} bytes)" : "";

            return $"TCP Message {direction}{size}";
        }
    }
}

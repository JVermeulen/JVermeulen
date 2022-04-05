using Google.Protobuf;
using System;

namespace JVermeulen.Monitoring
{
    public static class Parser
    {
        public static T Parse<T>(byte[] data, int length) where T : IMessage, new()
        {
            T message = new T();

            message.MergeFrom(data, 0, length);

            return message;
        }

        public static byte[] Parse(IMessage message)
        {
            return message.ToByteArray();
        }
    }
}

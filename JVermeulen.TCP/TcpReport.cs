using System;

namespace JVermeulen.TCP
{
    public class TcpReport
    {
        public DateTime StartedAt { get; set; }
        public DateTime StoppedAt { get; set; }
        public TimeSpan Duration => StartedAt != default && StoppedAt != default ? StoppedAt - StartedAt : default;

        public long NumberOfConnectedClients { get; set; }
        public long NumberOfDisconnectedClients { get; set; }
        public long NumberOfBytesReceived { get; set; }
        public long NumberOfBytesSent { get; set; }
        public long NumberOfMessagesReceived { get; set; }
        public long NumberOfMessagesSent { get; set; }

        public double AverageConnectedClients => Duration != default ? NumberOfConnectedClients / Duration.TotalSeconds : 0;
        public double AverageDisconnectedClients => Duration != default ? NumberOfDisconnectedClients / Duration.TotalSeconds : 0;
        public double AverageBytesReceived => Duration != default ? NumberOfBytesReceived / Duration.TotalSeconds : 0;
        public double AverageBytesSent => Duration != default ? NumberOfBytesSent / Duration.TotalSeconds : 0;
        public double AverageMessagesReceived => Duration != default ? NumberOfMessagesReceived / Duration.TotalSeconds : 0;
        public double AverageMessagesSent => Duration != default ? NumberOfMessagesSent / Duration.TotalSeconds : 0;

        public TcpReport()
        {
            StartedAt = DateTime.Now;
        }

        public override string ToString()
        {
            var lines = new string[]
            {
                "TCP Report:",
                $"- Connected clients: {NumberOfConnectedClients} ({AverageConnectedClients} /s)",
                $"- Disonnected clients: {NumberOfDisconnectedClients} ({AverageDisconnectedClients} /s)",
                $"- Bytes received: {NumberOfBytesReceived} ({AverageBytesReceived} B/s)",
                $"- Bytes sent: {NumberOfBytesSent} ({AverageBytesSent} B/s)",
                $"- Messages received: {NumberOfMessagesReceived} ({AverageMessagesReceived} /s)",
                $"- Messages sent: {NumberOfMessagesSent} ({AverageMessagesSent} /s)",
            };

            return string.Join("\r\n", lines);
        }
    }
}

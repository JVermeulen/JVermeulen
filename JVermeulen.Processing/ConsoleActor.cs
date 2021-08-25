using System;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Reads from and writes to the Console.
    /// </summary>
    public class ConsoleActor : Actor
    {
        /// <summary>
        /// Process messages from the Inbox.
        /// </summary>
        /// <param name="message">The message to process.</param>
        protected override void OnReceive(SessionMessage message)
        {
            if (message.ContentIsTypeof<SessionStatus>())
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (message.ContentIsTypeof<Heartbeat>())
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (message.ContentIsTypeof<ContentMessage<string>>())
                Console.ForegroundColor = ConsoleColor.Cyan;
            else
                Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine($"{message.CreatedAt:T} {message}");

            Console.ResetColor();
        }

        /// <summary>
        /// Sends the new status to the Outbox.
        /// </summary>
        protected override void OnStarting()
        {
            base.OnStarting();

            Read();
        }

        private void Read()
        {
            if (Status == SessionStatus.Starting || Status == SessionStatus.Started)
                GetInputAsync().ContinueWith(value => OnRead(value.Result));
        }

        private void OnRead(string value)
        {
            var message = new ContentMessage<string>(null, null, true, true, value);
            Outbox.Add(new SessionMessage(this, message));

            Read();
        }

        private static async Task<string> GetInputAsync()
        {
            return await Task.Run(() => Console.ReadLine());
        }

        /// <summary>
        /// A String that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return $"Console";
        }
    }
}

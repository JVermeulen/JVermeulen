using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A group of actors that distribute content messages.
    /// </summary>
    public class ActorDistributor : IDisposable
    {
        /// <summary>
        /// A list of actors.
        /// </summary>
        public List<Actor> Actors { get; set; }

        /// <summary>
        /// A list of subscriptions from the MessageBox(s).
        /// </summary>
        private List<IDisposable> Subscriptions { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        public ActorDistributor()
        {
            Actors = new List<Actor>();
            Subscriptions = new List<IDisposable>();
        }

        /// <summary>
        /// Add an actor.
        /// </summary>
        /// <param name="actor">The actor to add.</param>
        public void Add(Actor actor)
        {
            var subscription = actor.Outbox.SubscribeSafe(OnReceive);

            Subscriptions.Add(subscription);

            Actors.Add(actor);
        }

        /// <summary>
        /// A message has been received.
        /// </summary>
        /// <param name="message">The received message.</param>
        private void OnReceive(SessionMessage message)
        {
            if (message.ContentIsTypeof<SessionStatus, ContentMessage<string>, SessionMessage>())
            {
                var destinations = Actors.Where(a => a != message.Sender);

                foreach (var actors in destinations)
                {
                    actors.Inbox.Add(message);
                }
            }
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Subscriptions.ForEach(s => s.Dispose());
        }
    }
}

using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A session that contains a SubscriptionQueue running on the given Scheduler.
    /// </summary>
    public class SubscriptionSession : Session
    {
        /// <summary>
        /// The Scheduler that handles messages.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <summary>
        /// The SubscriptionQueue to subscribe to new messages.
        /// </summary>
        public SubscriptionQueue<SessionMessage> Queue { get; private set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="scheduler">The Scheduler that handles messages.</param>
        public SubscriptionSession(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Queue = new SubscriptionQueue<SessionMessage>(Scheduler);
        }

        /// <summary>
        /// Sends the new status to the Queue.
        /// </summary>
        protected override void OnStarting()
        {
            base.OnStarting();

            Queue.Enqueue(new SessionMessage(this, Status));
        }

        /// <summary>
        /// Sends the new status to the Queue.
        /// </summary>
        protected override void OnStarted()
        {
            base.OnStarted();

            Queue.Enqueue(new SessionMessage(this, Status));
        }

        /// <summary>
        /// Sends the new status to the Queue.
        /// </summary>
        protected override void OnStopping()
        {
            OnStopping();

            Queue.Enqueue(new SessionMessage(this, Status));
        }

        /// <summary>
        /// Sends the new status to the Queue.
        /// </summary>
        protected override void OnStopped()
        {
            OnStopped();

            Queue.Enqueue(new SessionMessage(this, Status));
        }

        /// <summary>
        /// Subscribe to the Queue.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Queue.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        public virtual IDisposable Subscribe(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return Queue.Subscribe(onNext, onError);
        }
    }
}

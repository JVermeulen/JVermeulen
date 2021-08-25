using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Concurrent processing of messages using the Actor model.
    /// </summary>
    public class Actor : Session, IObservable<SessionMessage>
    {
        /// <summary>
        /// A list of subscriptions from the MessageBox(s).
        /// </summary>
        protected List<IDisposable> Subscriptions { get; set; }

        /// <summary>
        /// The MessageBox for incoming messages.
        /// </summary>
        public MessageBox<SessionMessage> Inbox { get; private set; }

        /// <summary>
        /// The MessageBox for outgoing messages.
        /// </summary>
        public MessageBox<SessionMessage> Outbox { get; private set; }

        /// <summary>
        /// When true, generated heartbeats are send to the Outbox. Default is false.
        /// </summary>
        public bool OptionSendHeartbeatToOutbox { get; set; } = false;

        /// <summary>
        /// When true, status changes are send to the Outbox. Default is true.
        /// </summary>
        public bool OptionSendStatusChangedToOutbox { get; set; } = true;

        /// <summary>
        /// Generates heartbeat at the given interval.
        /// </summary>
        private HeartbeatProvider HeartbeatProvider { get; set; }

        /// <summary>
        /// The interval between heartbeats. Requires (re)start.
        /// </summary>
        public TimeSpan OptionHeartbeatInterval { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="heartbeatInterval">The time between heartbeats. When default, no heartbeats will be generated.</param>
        /// <param name="scheduler">The scheduler of the Outbox. When null, a new EventLoopScheduler is used.</param>
        public Actor(TimeSpan heartbeatInterval = default, IScheduler scheduler = null)
        {
            Subscriptions = new List<IDisposable>();

            OptionHeartbeatInterval = heartbeatInterval;

            Inbox = new MessageBox<SessionMessage>(new EventLoopScheduler());
            Subscriptions.Add(Inbox.SubscribeSafe(OnReceive));

            Outbox = new MessageBox<SessionMessage>(scheduler ?? new EventLoopScheduler());
        }

        /// <summary>
        /// Process messages from the Inbox.
        /// </summary>
        /// <param name="message">The message to process.</param>
        protected virtual void OnReceive(SessionMessage message) { }

        /// <summary>
        /// Subscribe to the Outbox.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        public virtual IDisposable Subscribe(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return Outbox.SubscribeSafe(onNext, onError);
        }

        /// <summary>
        /// Sends the new status to the Outbox.
        /// </summary>
        protected override void OnStarting()
        {
            base.OnStarting();

            if (OptionSendStatusChangedToOutbox)
                Outbox.Add(new SessionMessage(this, Status));

            if (OptionHeartbeatInterval != default)
            {
                HeartbeatProvider = new HeartbeatProvider(OptionHeartbeatInterval, Inbox.Scheduler);
                Subscriptions.Add(HeartbeatProvider.SubscribeSafe(OnHeartbeat));
            }
        }

        /// <summary>
        /// Sends the new status to the Outbox.
        /// </summary>
        protected override void OnStarted()
        {
            base.OnStarted();

            if (OptionSendStatusChangedToOutbox)
                Outbox.Add(new SessionMessage(this, Status));
        }

        /// <summary>
        /// Sends the new status to the Outbox.
        /// </summary>
        protected override void OnStopping()
        {
            base.OnStopping();

            HeartbeatProvider?.Dispose();

            if (OptionSendStatusChangedToOutbox)
                Outbox.Add(new SessionMessage(this, Status));
        }

        /// <summary>
        /// Sends the new status to the Outbox.
        /// </summary>
        protected override void OnStopped()
        {
            base.OnStopped();

            if (OptionSendStatusChangedToOutbox)
                Outbox.Add(new SessionMessage(this, Status));
        }

        /// <summary>
        /// A heartbeat has been generated.
        /// </summary>
        /// <param name="heartbeat">The heartbeat message.</param>
        protected virtual void OnHeartbeat(Heartbeat heartbeat)
        {
            if (OptionSendHeartbeatToOutbox)
                Outbox.Add(new SessionMessage(this, heartbeat));
        }

        /// <summary>
        /// Subscribes a message handler.
        /// </summary>
        /// <param name="observer">The handler.</param>
        public IDisposable Subscribe(IObserver<SessionMessage> observer)
        {
            return Outbox.Subscribe(observer);
        }

        /// <summary>
        /// Subscribe to the Outbox.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <param name="query">A function to test each source element for a condition.</param>
        public IDisposable SubscribeSafe(Action<SessionMessage> onNext, Action<Exception> onError = null, Func<SessionMessage, bool> query = null)
        {
            return Outbox.SubscribeSafe(onNext, onError, query);
        }

        /// <summary>
        /// Subscribe to the actor. TSender is the Sender type to check for.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        public IDisposable SubscribeSafe<TSender>(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return SubscribeSafe(onNext, onError, m => m.SenderIsTypeOf<TSender>());
        }

        /// <summary>
        /// Subscribe to the actor. TSender/TValue is the Sender/Value type to check for.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        public IDisposable SubscribeSafe<TSender, TValue>(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return SubscribeSafe(onNext, onError, m => m.SenderIsTypeOf<TSender>() && m.ContentIsTypeof<TValue>());
        }

        /// <summary>
        /// Subscribe to the actor. TSender/TValue is the Sender/Value type to check for.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        public IDisposable SubscribeSafe<TSender, TValue1, TValue2>(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return SubscribeSafe(onNext, onError, m => m.SenderIsTypeOf<TSender>() && m.ContentIsTypeof<TValue1, TValue2>());
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            Subscriptions.ForEach(s => s.Dispose());
        }
    }
}

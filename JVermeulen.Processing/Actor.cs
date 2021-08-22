using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Concurrent processing of messages using the Actor model.
    /// </summary>
    public class Actor : Session
    {
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
        /// Internal heartbeat generator.
        /// </summary>
        private HeartbeatBox Heart { get; set; }

        /// <summary>
        /// The interval between heartbeats. Requires (re)start.
        /// </summary>
        public TimeSpan HeartbeatInterval { get => Heart.Interval; set => Heart.Interval = value; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="heartbeatInterval">The time between heartbeats. When default, no heartbeats will be generated.</param>
        /// <param name="scheduler">The scheduler of the Outbox. When null, a new EventLoopScheduler is used.</param>
        public Actor(TimeSpan heartbeatInterval = default, IScheduler scheduler = null)
        {
            Inbox = new MessageBox<SessionMessage>(new EventLoopScheduler());
            Inbox.Subscribe(OnReceived);

            Outbox = new MessageBox<SessionMessage>(scheduler ?? new EventLoopScheduler());

            Heart = new HeartbeatBox(Inbox.Scheduler, heartbeatInterval);
            Heart.Subscribe(OnHeartbeat);
        }

        /// <summary>
        /// Process messages from the Inbox.
        /// </summary>
        /// <param name="message">The message to process.</param>
        protected virtual void OnReceived(SessionMessage message) { }

        /// <summary>
        /// Subscribe to the Outbox.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        public virtual IDisposable Subscribe(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return Outbox.Subscribe(onNext, onError);
        }

        /// <summary>
        /// Sends the new status to the Outbox.
        /// </summary>
        protected override void OnStarting()
        {
            base.OnStarting();

            if (OptionSendStatusChangedToOutbox)
                Outbox.Add(new SessionMessage(this, Status));

            Heart.Start(HeartbeatInterval);
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

            Heart.Stop();

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
    }
}

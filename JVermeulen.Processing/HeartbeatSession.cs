using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A session that generates heartbeats with the given interval, on the given scheduler.
    /// </summary>
    public class HeartbeatSession : SubscriptionSession
    {
        /// <summary>
        /// The interval between heartbeats.
        /// </summary>
        public TimeSpan Interval { get; private set; }

        private CancellationTokenSource Cancellation { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="interval">The interval between heartbeats.</param>
        /// <param name="scheduler">The scheduler to process the heartbeats.</param>
        public HeartbeatSession(TimeSpan interval, IScheduler scheduler = null) : base(scheduler)
        {
            Interval = interval;
        }

        /// <summary>
        /// Start generating heartbeats.
        /// </summary>
        protected override void OnStarting()
        {
            base.OnStarting();

            Cancellation = new CancellationTokenSource();

            Observable
                .Interval(Interval)
                .ObserveOn(Scheduler)
                .Subscribe(OnHeartbeating, Cancellation.Token);
        }

        /// <summary>
        /// Stop generating heartbeats.
        /// </summary>
        protected override void OnStopping()
        {
            base.OnStopping();

            Cancellation?.Cancel();
        }

        private void OnHeartbeating(long count)
        {
            var heartbeat = new Heartbeat(count);

            Queue.Enqueue(new SessionMessage(this, heartbeat));

            OnHeartbeat(count);
        }

        /// <summary>
        /// A heartbeat occured.
        /// </summary>
        /// <param name="count">The incremented number of this heartbeat, starting with 0.</param>
        protected virtual void OnHeartbeat(long count) { }
    }
}

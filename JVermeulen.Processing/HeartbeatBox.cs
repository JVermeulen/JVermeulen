using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Generates heartbeat at the given interval.
    /// </summary>
    public class HeartbeatBox : MessageBox<Heartbeat>
    {
        /// <summary>
        /// Returs true when started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// The interval between heartbeats. When default, no heartbeats will be generated. Requires (re)start after change.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Internal cancel.
        /// </summary>
        private CancellationTokenSource Cancellation { get; set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="scheduler">The Scheduler to use.</param>
        /// <param name="interval">The interval between heartbeats. When default, no heartbeats will be generated.</param>
        public HeartbeatBox(IScheduler scheduler = null, TimeSpan interval = default) : base(scheduler)
        {
            Interval = interval;
        }

        /// <summary>
        /// Start generating heartbeats.
        /// </summary>
        /// <param name="interval"></param>
        public void Start(TimeSpan interval)
        {
            if (!IsStarted && interval != default)
            {
                Cancellation = new CancellationTokenSource();

                Observable
                    .Interval(Interval)
                    .ObserveOn(Scheduler)
                    .Subscribe((e) => Add(new Heartbeat(e)), Cancellation.Token);

                IsStarted = true;
            }
        }

        /// <summary>
        /// Stop generating heartbeats.
        /// </summary>
        public void Stop()
        {
            if (IsStarted)
            {
                Cancellation.Cancel();

                IsStarted = false;
            }
        }
    }
}

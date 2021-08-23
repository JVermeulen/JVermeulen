using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// Generates heartbeat at the given interval.
    /// </summary>
    public class HeartbeatProvider : MessageBox<Heartbeat>
    {
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
        /// <param name="interval">The interval between heartbeats. When default, no heartbeats will be generated.</param>
        /// <param name="scheduler">The Scheduler to use.</param>
        public HeartbeatProvider(TimeSpan interval, IScheduler scheduler)
        {
            Interval = interval;

            Cancellation = new CancellationTokenSource();

            Observable
                .Interval(Interval)
                .ObserveOn(scheduler)
                .Subscribe((e) => Add(new Heartbeat(e)), Cancellation.Token);
        }

        /// <summary>
        /// Dispose this object.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            Cancellation.Cancel();
        }
    }
}

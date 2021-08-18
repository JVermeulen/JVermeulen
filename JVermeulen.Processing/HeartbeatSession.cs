using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class HeartbeatSession : SubscriptionSession
    {
        public TimeSpan Interval { get; private set; }
        private CancellationTokenSource Cancellation { get; set; }

        public HeartbeatSession(TimeSpan interval, IScheduler scheduler = null) : base(scheduler)
        {
            Interval = interval;
        }

        public override void OnStarting()
        {
            base.OnStarting();

            Cancellation = new CancellationTokenSource();

            Observable
                .Interval(Interval)
                .ObserveOn(Scheduler)
                .Subscribe(OnHeartbeat, Cancellation.Token);
        }

        public override void OnStopping()
        {
            base.OnStopping();

            Cancellation?.Cancel();
        }

        private void OnHeartbeat(long count)
        {
            var heartbeat = new Heartbeat(count);

            Queue.Enqueue(new SessionMessage(this, heartbeat));

            OnHeartbeatReceived(count);
        }

        public virtual void OnHeartbeatReceived(long count) { }
    }
}

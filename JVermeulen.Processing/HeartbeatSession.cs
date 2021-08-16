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
    public class HeartbeatSession : Session
    {
        public TimeSpan Interval { get; private set; }

        private SubscriptionQueue<long> Queue { get; set; }
        private CancellationTokenSource Cancellation { get; set; }

        public HeartbeatSession(TimeSpan interval, IScheduler scheduler = null) : base(scheduler ?? new EventLoopScheduler())
        {
            Interval = interval;

            Queue = new SubscriptionQueue<long>(scheduler);
        }

        public override void OnStarting()
        {
            Cancellation = new CancellationTokenSource();

            Observable
                .Interval(Interval)
                .ObserveOn(Scheduler)
                .Subscribe(Queue.Enqueue, Cancellation.Token);
        }

        public IDisposable Subscribe(Action<long> onNext, Action<Exception> onError = null)
        {
            return Queue.Subscribe(onNext, onError);
        }

        public override void OnStopping()
        {
            Cancellation?.Cancel();
        }
    }
}

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
    public class IntervalGenerator : SubscriptionQueue<long>, IStartable
    {
        public bool IsStarted { get; set; }
        public TimeSpan Interval { get; private set; }
        
        private CancellationTokenSource Cancellation { get; set; }

        public IntervalGenerator(TimeSpan interval, IScheduler scheduler = null) : base(scheduler ?? new EventLoopScheduler())
        {
            Interval = interval;
        }

        public void Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;

                Cancellation = new CancellationTokenSource();

                Observable
                    .Interval(Interval)
                    .ObserveOn(Scheduler)
                    .Subscribe(Add, Cancellation.Token);
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                Cancellation?.Cancel();

                IsStarted = false;
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }
    }
}

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
    public class HeartbeatGenerator : Inbox<Heartbeat>, IStartable
    {
        public TimeSpan Interval { get; private set; }

        private CancellationTokenSource Cancellation { get; set; }

        public TimeCounter Timer { get; private set; }
        public bool IsStarted => Timer.IsStarted;

        public HeartbeatGenerator(TimeSpan interval, IScheduler scheduler = null) : base(scheduler ?? new EventLoopScheduler())
        {
            Interval = interval;

            Timer = new TimeCounter();
        }

        public void Start()
        {
            if (!IsStarted)
            {
                Cancellation = new CancellationTokenSource();

                Observable
                    .Interval(Interval)
                    .ObserveOn(Scheduler)
                    .Subscribe(OnNext, Cancellation.Token);

                Timer.Start();
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                Cancellation?.Cancel();

                Timer.Stop();
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void OnNext(long count)
        {
            var heartbeat = new Heartbeat(count);

            Send(heartbeat);
        }
    }
}

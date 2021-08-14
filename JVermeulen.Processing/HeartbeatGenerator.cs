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
    public class HeartbeatGenerator : Inbox<Heartbeat>, IStartStop
    {
        public TimeSpan Interval { get; private set; }

        private long Value { get; set; }
        private CancellationTokenSource Cancellation { get; set; }

        private TimeCounter Timer { get; set; }

        public HeartbeatGenerator(TimeSpan interval, IScheduler scheduler = null) : base(scheduler ?? new EventLoopScheduler())
        {
            Interval = interval;

            Timer = new TimeCounter();
        }

        public void Start()
        {
            Cancellation = new CancellationTokenSource();

            Observable
                .Interval(Interval)
                .ObserveOn(Scheduler)
                .Subscribe(OnNext, Cancellation.Token);

            Timer.Start();
        }

        public void Stop()
        {
            Timer.Start();

            Cancellation?.Cancel();
        }

        public void Restart()
        {
            if (Timer.IsStarted)
                Stop();

            Start();
        }

        private void OnNext(long count)
        {
            var heartbeat = new Heartbeat(Value++);

            Send(heartbeat);
        }
    }
}

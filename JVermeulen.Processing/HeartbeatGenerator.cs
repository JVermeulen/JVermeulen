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
    public class HeartbeatGenerator : Generator<Heartbeat>
    {
        public TimeSpan Interval { get; private set; }

        private long Value { get; set; }
        private CancellationTokenSource Cancellation { get; set; }

        public HeartbeatGenerator(string name, TimeSpan interval) : base(name)
        {
            Interval = interval;
        }

        public void Start(IScheduler scheduler = null)
        {
            base.Start();

            if (scheduler == null)
                scheduler = new EventLoopScheduler();

            Cancellation = new CancellationTokenSource();

            Observable
                .Interval(Interval)
                .ObserveOn(scheduler)
                .Subscribe(OnNext, Cancellation.Token);

            OnNext(Value);
        }

        public override void Stop()
        {
            base.Stop();

            Cancellation?.Cancel();
        }

        private void OnNext(long count)
        {
            var value = new Heartbeat(Name, Value++);

            Send(value);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}

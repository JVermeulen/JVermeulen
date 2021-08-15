using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public abstract class SubscriptionQueue<T> : IDisposable
    {
        protected IScheduler Scheduler { get; set; }
        private Subject<T> Values { get; set; }
        internal IObservable<T> Queue => Values.ObserveOn(Scheduler).AsObservable();

        private ValueCounter ProcessedValuesCounter { get; set; }
        private ValueCounter PendingValuesCounter { get; set; }
        public long NumberOfValuesPending => (long)PendingValuesCounter.Value;
        public long NumberOfValuesProcessed => (long)ProcessedValuesCounter.Value;

        public SubscriptionQueue(IScheduler scheduler)
        {
            Scheduler = scheduler;

            Values = new Subject<T>();
            ProcessedValuesCounter = new ValueCounter();
            PendingValuesCounter = new ValueCounter();
            Queue.Subscribe(OnReceive);
        }

        public void Add(T value)
        {
            PendingValuesCounter.Increment();

            Values.OnNext(value);
        }

        private void OnReceive(T value)
        {
            PendingValuesCounter.Decrement();
            ProcessedValuesCounter.Increment();
        }

        public void Dispose()
        {
            Values?.Dispose();
        }
    }
}

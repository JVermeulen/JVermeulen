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
    public class Inbox<T> : IDisposable
    {
        protected IScheduler Scheduler { get; set; }
        private Subject<T> Value { get; set; }
        public IObservable<T> OnReceive => Value.ObserveOn(Scheduler).AsObservable();
        
        private ValueCounter Counter { get; set; }
        public int Count => (int)Counter.Value;

        public Inbox(IScheduler scheduler)
        {
            Scheduler = scheduler;

            Value = new Subject<T>();
            Counter = new ValueCounter(0);
            OnReceive.Subscribe(m => Counter.Decrement());
        }

        public void Send(T value)
        {
            Counter.Increment();

            Value.OnNext(value);
        }

        public void Dispose()
        {
            Value?.Dispose();
        }
    }
}

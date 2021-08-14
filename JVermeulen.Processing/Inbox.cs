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
        private IScheduler Scheduler { get; set; }
        private Subject<T> Value { get; set; }
        public IObservable<T> OnReceive => Value.ObserveOn(Scheduler).AsObservable();

        public Inbox(IScheduler scheduler)
        {
            Scheduler = scheduler;

            Value = new Subject<T>();
        }

        public void Send(T value)
        {
            Value.OnNext(value);
        }

        public void Dispose()
        {
            Value?.Dispose();
        }
    }
}

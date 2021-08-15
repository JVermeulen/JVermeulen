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
    public abstract class Inbox<T> : IDisposable
    {
        protected IScheduler Scheduler { get; set; }
        private Subject<T> Value { get; set; }
        public IObservable<T> OnReceive => Value.ObserveOn(Scheduler).AsObservable();

        private ValueCounter ProcessedTasksCounter { get; set; }
        private ValueCounter PendingTasksCounter { get; set; }
        public long NumberOfPendingTasks => (long)PendingTasksCounter.Value;
        public long NumberOfProcessedTasks => (long)ProcessedTasksCounter.Value;

        public Inbox(IScheduler scheduler)
        {
            Scheduler = scheduler;

            Value = new Subject<T>();
            ProcessedTasksCounter = new ValueCounter();
            PendingTasksCounter = new ValueCounter();
            OnReceive.Subscribe(OnReceived);
        }

        public void Send(T value)
        {
            PendingTasksCounter.Increment();

            Value.OnNext(value);
        }

        private void OnReceived(T value)
        {
            PendingTasksCounter.Decrement();
            ProcessedTasksCounter.Increment();
        }

        public void Dispose()
        {
            Value?.Dispose();
        }
    }
}

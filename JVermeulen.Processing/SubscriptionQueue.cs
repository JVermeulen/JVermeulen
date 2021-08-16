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
    public class SubscriptionQueue<T> : IDisposable
    {
        protected IScheduler Scheduler { get; set; }
        private Subject<T> Value { get; set; }
        protected IObservable<T> Queue => Value.ObserveOn(Scheduler).AsObservable();

        private ValueCounter ProcessedValuesCounter { get; set; }
        private ValueCounter PendingValuesCounter { get; set; }

        public long NumberOfValuesPending => (long)PendingValuesCounter.Value;
        public long NumberOfValuesProcessed => (long)ProcessedValuesCounter.Value;

        public SubscriptionQueue(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Value = new Subject<T>();

            ProcessedValuesCounter = new ValueCounter();
            PendingValuesCounter = new ValueCounter();

            Queue.Subscribe(OnDequeue);
        }

        private static Action<T> ActionAndCatch(Action<T> action, Action<Exception> catchAction)
        {
            return item =>
            {
                try
                {
                    action(item);
                }
                catch (Exception ex)
                {
                    catchAction(ex);
                }
            };
        }

        public IDisposable Subscribe(Action<T> onNext, Action<Exception> onError = null)
        {
            return Queue.Subscribe(ActionAndCatch(onNext, onError ?? OnError), OnError);
        }

        public void Enqueue(T value)
        {
            PendingValuesCounter.Increment();

            if (!Value.IsDisposed)
                Value.OnNext(value);
        }

        private void OnDequeue(T value)
        {
            PendingValuesCounter.Decrement();
            ProcessedValuesCounter.Increment();
        }

        private void OnError(Exception ex)
        {
            // Ignore and continue
        }

        public void Dispose()
        {
            Value?.Dispose();
        }
    }
}

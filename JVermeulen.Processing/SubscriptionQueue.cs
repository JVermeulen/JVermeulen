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

        public long NumberOfValuesPending => PendingValuesCounter.Value;
        public long NumberOfValuesProcessed => ProcessedValuesCounter.Value;

        public bool OptionWriteToConsole { get; set; }

        public SubscriptionQueue(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Value = new Subject<T>();

            ProcessedValuesCounter = new ValueCounter();
            PendingValuesCounter = new ValueCounter();

            Queue.Subscribe(OnDequeue);
        }

        public IDisposable Subscribe(Action<T> onNext, Action<Exception> onError = null)
        {
            return Subscribe(Queue, onNext, onError ?? OnError);
        }

        public static IDisposable Subscribe(IObservable<T> queue, Action<T> onNext, Action<Exception> onError)
        {
            return queue.Subscribe(ActionAndCatch(onNext, onError ?? onError), onError);
        }

        public static Action<T> ActionAndCatch(Action<T> action, Action<Exception> catchAction)
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

            if (OptionWriteToConsole)
                Console.WriteLine($"{DateTime.Now:T} {value}");
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

using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A queue of messages you can subscribe to. Message are handled one-by-one.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public class SubscriptionQueue<T> : IDisposable
    {
        /// <summary>
        /// The Scheduler that handles messages.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <summary>
        /// The internal value used for the Queue.
        /// </summary>
        private Subject<T> Value { get; set; }

        /// <summary>
        /// The internal queue to subscribe to.
        /// </summary>
        protected IObservable<T> Queue => Value.ObserveOn(Scheduler).AsObservable();

        /// <summary>
        /// Counts the processed messages from the queue.
        /// </summary>
        private ValueCounter ProcessedValuesCounter { get; set; }

        /// <summary>
        /// Counts the pending messages in the Queue.
        /// </summary>
        private ValueCounter PendingValuesCounter { get; set; }

        /// <summary>
        /// The number of processed messages from the queue.
        /// </summary>
        public long NumberOfValuesPending => PendingValuesCounter.Value;

        /// <summary>
        /// The number of pending messages in the Queue.
        /// </summary>
        public long NumberOfValuesProcessed => ProcessedValuesCounter.Value;

        /// <summary>
        /// When true, processed messages are send to the Console. Default is false.
        /// </summary>
        public bool OptionWriteToConsole { get; set; } = false;

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="scheduler">The Scheduler that handles messages.</param>
        public SubscriptionQueue(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Value = new Subject<T>();

            ProcessedValuesCounter = new ValueCounter();
            PendingValuesCounter = new ValueCounter();

            Queue.Subscribe(OnDequeue);
        }

        /// <summary>
        /// Subscribe to the Queue.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Queue.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <returns></returns>
        public IDisposable Subscribe(Action<T> onNext, Action<Exception> onError = null)
        {
            return Subscribe(Queue, onNext, onError ?? OnError);
        }

        /// <summary>
        /// Subscribe to the given Queue.
        /// </summary>
        /// <param name="queue">A queue of messages you can subscribe to.</param>
        /// <param name="onNext">What to do with messages received from Queue.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <returns></returns>
        public static IDisposable Subscribe(IObservable<T> queue, Action<T> onNext, Action<Exception> onError)
        {
            return queue.Subscribe(ActionAndCatch(onNext, onError ?? onError), onError);
        }

        /// <summary>
        /// Cathes exceptions occured in the action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        /// <param name="catchAction">What to do with errors occured in the action.</param>
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

        /// <summary>
        /// Sends the given message to the Queue and to the subscribers.
        /// </summary>
        /// <param name="value">The message to send.</param>
        public void Enqueue(T value)
        {
            PendingValuesCounter.Increment();

            if (!Value.IsDisposed)
                Value.OnNext(value);
        }

        /// <summary>
        /// Internal subscription to the queue.
        /// </summary>
        /// <param name="value">The received message.</param>
        private void OnDequeue(T value)
        {
            PendingValuesCounter.Decrement();
            ProcessedValuesCounter.Increment();

            if (OptionWriteToConsole)
                Console.WriteLine($"{DateTime.Now:T} {value}");
        }

        /// <summary>
        /// What to do with errors occured in the action.
        /// </summary>
        /// <param name="ex">The occured exception.</param>
        private void OnError(Exception ex)
        {
            // Ignore and continue
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Value?.Dispose();
        }
    }
}

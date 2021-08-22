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
    public class MessageBox<T> : IDisposable
    {
        /// <summary>
        /// The Scheduler that handles messages.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <summary>
        /// The internal message used for the Queue.
        /// </summary>
        private Subject<T> Messages { get; set; }

        /// <summary>
        /// The internal queue to subscribe to.
        /// </summary>
        public IObservable<T> Observer => Messages.ObserveOn(Scheduler).AsObservable();

        /// <summary>
        /// Counts the processed messages from the queue.
        /// </summary>
        private ValueCounter ProcessedMessageCounter { get; set; }

        /// <summary>
        /// Counts the pending messages in the Queue.
        /// </summary>
        private ValueCounter PendingMessageCounter { get; set; }

        /// <summary>
        /// The number of processed messages from the queue.
        /// </summary>
        public long NumberOfMessagesPending => PendingMessageCounter.Value;

        /// <summary>
        /// The number of pending messages in the Queue.
        /// </summary>
        public long NumberOfMessagesProcessed => ProcessedMessageCounter.Value;

        /// <summary>
        /// When true, processed messages are send to the Console. Default is false.
        /// </summary>
        public bool OptionWriteToConsole { get; set; } = false;

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="scheduler">The Scheduler that handles messages.</param>
        public MessageBox(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Messages = new Subject<T>();

            ProcessedMessageCounter = new ValueCounter();
            PendingMessageCounter = new ValueCounter();

            Observer.Subscribe(OnReceive);
        }

        /// <summary>
        /// Filters the elements of an observable sequence based on a predicate.
        /// </summary>
        /// <param name="where">A function to test each source element for a condition.</param>
        /// <param name="onNext">What to do with messages received from Queue.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <returns></returns>
        public IDisposable Where(Func<T, bool> where, Action<T> onNext, Action<Exception> onError = null)
        {
            return Observer.Where(where).Subscribe(onNext, onError);
        }

        /// <summary>
        /// Subscribe to the Queue.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Queue.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <returns></returns>
        public IDisposable Subscribe(Action<T> onNext, Action<Exception> onError = null)
        {
            return Observer.Subscribe(onNext, onError);
        }

        /// <summary>
        /// Subscribe to the given Queue.
        /// </summary>
        /// <param name="observer">A queue of messages you can subscribe to.</param>
        /// <param name="onNext">What to do with messages received from Queue.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <returns></returns>
        public static IDisposable Subscribe(this IObservable<T> observer, Action<T> onNext, Action<Exception> onError)
        {
            return observer.Subscribe(ActionAndCatch(onNext, onError), onError);
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
        /// <param name="message">The message to send.</param>
        public void Add(T message)
        {
            PendingMessageCounter.Increment();

            if (!Messages.IsDisposed)
                Messages.OnNext(message);
        }

        /// <summary>
        /// Internal subscription to the queue.
        /// </summary>
        /// <param name="message">The received message.</param>
        private void OnReceive(T message)
        {
            PendingMessageCounter.Decrement();
            ProcessedMessageCounter.Increment();

            if (OptionWriteToConsole)
                Console.WriteLine($"{DateTime.Now:T} {message}");
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
            Messages?.Dispose();
        }
    }
}

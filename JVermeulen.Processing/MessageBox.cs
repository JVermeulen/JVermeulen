using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A queue of messages you can subscribe to. Message are handled one-by-one.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public class MessageBox<T> : IObservable<T>, IDisposable
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
        /// The number of processed messages from the queue.
        /// </summary>
        public long NumberOfMessagesPending => Interlocked.Read(ref _NumberOfMessagesPending);
        private long _NumberOfMessagesPending;

        /// <summary>
        /// The number of pending messages in the Queue.
        /// </summary>
        public long NumberOfMessagesProcessed => Interlocked.Read(ref _NumberOfMessagesProcessed);
        private long _NumberOfMessagesProcessed;

        /// <summary>
        /// When true, processed messages are send to the Console. Default is false.
        /// </summary>
        public bool OptionWriteToConsole { get; set; } = false;

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        /// <param name="scheduler">The Scheduler that handles messages. When null, a new EventLoopScheduler will be created.</param>
        public MessageBox(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Messages = new Subject<T>();

            Observer.Subscribe(OnReceive);
        }

        /// <summary>
        /// Subscribes a message handler.
        /// </summary>
        /// <param name="observer">The handler.</param>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Observer.SubscribeSafe(observer);
        }

        /// <summary>
        /// Subscribe to the observer.
        /// </summary>
        /// <param name="onNext">What to do with messages received from Outbox.</param>
        /// <param name="onError">What to do with errors occured in the onNext action.</param>
        /// <param name="where">A function to test each source element for a condition.</param>
        public IDisposable SubscribeSafe(Action<T> onNext, Action<Exception> onError = null, Func<T, bool> where = null)
        {
            Action<Exception> ignore = (e) => { };

            var observer = where != null ? Observer.Where(where) : Observer;

            return observer.Subscribe(ActionAndCatch(onNext, onError), onError ?? ignore);
        }

        /// <summary>
        /// Cathes exceptions occured in the action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        /// <param name="catchAction">What to do with errors occured in the action.</param>
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
                    catchAction?.Invoke(ex);
                }
            };
        }

        /// <summary>
        /// Sends the given message to the Queue and to the subscribers.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Add(T message)
        {
            Interlocked.Increment(ref _NumberOfMessagesPending);

            if (!Messages.IsDisposed)
                Messages.OnNext(message);
        }

        /// <summary>
        /// Internal subscription to the queue.
        /// </summary>
        /// <param name="message">The received message.</param>
        private void OnReceive(T message)
        {
            Interlocked.Decrement(ref _NumberOfMessagesPending);
            Interlocked.Increment(ref _NumberOfMessagesProcessed);

            if (OptionWriteToConsole)
                Console.WriteLine($"{DateTime.Now:T} {message}");
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
            Messages?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace JVermeulen.Processing
{
    public class SubscriptionSession : Session
    {
        public IScheduler Scheduler { get; private set; }
        public SubscriptionQueue<SessionMessage> Queue { get; private set; }

        public SubscriptionSession(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();
            Queue = new SubscriptionQueue<SessionMessage>(Scheduler);
        }

        public override void OnStarting()
        {
            Queue.Enqueue(new SessionMessage(this, Status));
        }

        public override void OnStarted()
        {
            Queue.Enqueue(new SessionMessage(this, Status));
        }

        public override void OnStopping()
        {
            Queue.Enqueue(new SessionMessage(this, Status));
        }

        public override void OnStopped()
        {
            Queue.Enqueue(new SessionMessage(this, Status));
        }

        public virtual IDisposable Subscribe(Action<SessionMessage> onNext, Action<Exception> onError = null)
        {
            return Queue.Subscribe(onNext, onError);
        }
    }
}

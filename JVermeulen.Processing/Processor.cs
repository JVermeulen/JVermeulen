using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public abstract class Processor : IDisposable
    {
        private HeartbeatGenerator Generator { get; set; }
        protected EventLoopScheduler Scheduler { get; set; }

        public virtual string Name => this.GetType().Name;

        public bool IsEnabled { get; private set; }
        public Inbox<object> Inbox { get; private set; }

        public Processor(TimeSpan heartbeatInterval = default(TimeSpan))
        {
            Scheduler = new EventLoopScheduler();

            Inbox = new Inbox<object>(Scheduler);
            Inbox.OnReceive.Subscribe(Work);

            if (heartbeatInterval != default(TimeSpan))
            {
                Generator = new HeartbeatGenerator(Name, heartbeatInterval);
                Generator.OnReceive.Subscribe(Inbox.Send);
            }
        }

        public virtual void Start()
        {
            IsEnabled = true;

            Generator?.Start(Scheduler);
        }

        public virtual void Stop()
        {
            IsEnabled = false;

            Generator?.Stop();
        }

        public virtual void Work(object value)
        {
            //
        }

        public void Dispose()
        {
            Stop();

            Scheduler?.Dispose();
            Inbox?.Dispose();
        }
    }
}

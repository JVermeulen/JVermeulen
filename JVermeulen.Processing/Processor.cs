using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public abstract class Processor : TimeCounter
    {
        private HeartbeatGenerator Generator { get; set; }
        protected EventLoopScheduler Scheduler { get; set; }

        public virtual string Name => this.GetType().Name;

        public Inbox<object> Inbox { get; private set; }

        public Processor(TimeSpan heartbeatInterval = default(TimeSpan)) : base(false)
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

        public override void Start()
        {
            base.Start();

            Generator?.Start(Scheduler);
        }

        public override void Stop()
        {
            base.Stop();

            Generator?.Stop();
        }

        public abstract void Work(object value);

        public override void Dispose()
        {
            Stop();

            Scheduler?.Dispose();
            Inbox?.Dispose();
        }
    }
}

using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public abstract class Processor : TimeCounter
    {
        private HeartbeatGenerator Generator { get; set; }
        protected EventLoopScheduler Scheduler { get; set; }

        public Inbox<object> Inbox { get; private set; }

        public Processor(TimeSpan heartbeatInterval = default) : base()
        {
            Scheduler = new EventLoopScheduler();

            Inbox = new Inbox<object>(Scheduler);
            Inbox.OnReceive.Subscribe(OnWork);

            if (heartbeatInterval != default)
            {
                Generator = new HeartbeatGenerator(heartbeatInterval);
                Generator.OnReceive.Subscribe(OnHeartbeat);
            }
        }

        public override void Start()
        {
            base.Start();

            Generator?.Start();

            OnStarted();
        }

        public override void Stop()
        {
            base.Stop();

            Generator?.Stop();

            OnStopped();
        }

        public abstract void OnWork(object value);
        public abstract void OnHeartbeat(Heartbeat heartbeat);
        public abstract void OnStarted();
        public abstract void OnStopped();

        public override void Dispose()
        {
            Stop();

            Scheduler?.Dispose();
            Inbox?.Dispose();
        }
    }
}

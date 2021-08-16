using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public abstract class Processor<T> : SubscriptionQueue<T>, ISession
    {
        protected Session Timer { get; private set; }
        protected IntervalGenerator Heartbeat { get; set; }
        public bool IsStarted => Timer.Status == SessionStatus.Started;

        public abstract void OnReceived(T value);
        public abstract void OnHeartbeat(long count);
        public abstract void OnStarting();
        public abstract void OnStarted();
        public abstract void OnStopping();
        public abstract void OnStopped();
        public abstract void OnExceptionOccured(Exception ex);

        public Processor() : base(new EventLoopScheduler())
        {
            Queue.Subscribe(OnReceive);

            Timer = new Session();
        }

        public void EnableHeartbeat(TimeSpan interval, bool syncScheduler)
        {
            if (Heartbeat == null)
            {
                Heartbeat = new IntervalGenerator(interval, syncScheduler ? Scheduler : new EventLoopScheduler());
                Heartbeat.Subscribe(OnHeartbeat);

                if (IsStarted)
                    Heartbeat.Start();
            }
        }

        public void DisableHeartbeat()
        {
            Heartbeat?.Dispose();
            Heartbeat = null;
        }

        private void OnReceive(T value)
        {
            try
            {
                OnReceived(value);

                if (!IsStarted && NumberOfValuesPending == 0)
                    OnStopped();
            }
            catch (Exception ex)
            {
                OnExceptionOccured(ex);
            }
        }

        public void Start()
        {
            if (!IsStarted)
            {
                OnStarting();

                Heartbeat?.Start();
                Timer?.Start();

                OnStarted();
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                OnStopping();

                Heartbeat?.Stop();
                Timer?.Stop();
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public new void Dispose()
        {
            Stop();

            base.Dispose();
        }
    }
}

using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public abstract class Processor : SubscriptionQueue<object>, IStartable
    {
        protected TimeCounter Timer { get; private set; }
        protected IntervalGenerator Heartbeat { get; set; }
        public bool IsStarted => Timer.IsStarted;

        public abstract void OnValueReceived(object value);
        public abstract void OnHeartbeat(long count);
        public abstract void OnExceptionOccured(Exception ex);
        public abstract void OnStarting();
        public abstract void OnStarted();
        public abstract void OnStopping();
        public abstract void OnStopped();

        public Processor() : base(new EventLoopScheduler())
        {
            Queue.Subscribe(OnNext);

            Timer = new TimeCounter();
        }

        public void EnableHeartbeat(TimeSpan interval, bool syncScheduler)
        {
            if (Heartbeat == null)
            {
                Heartbeat = new IntervalGenerator(interval, syncScheduler ? Scheduler : new EventLoopScheduler());
                Heartbeat.Queue.Subscribe(OnHeartbeat);

                if (IsStarted)
                    Heartbeat.Start();
            }
        }

        public void DisableHeartbeat()
        {
            Heartbeat?.Dispose();
            Heartbeat = null;
        }

        private void OnNext(object value)
        {
            try
            {
                OnValueReceived(value);

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

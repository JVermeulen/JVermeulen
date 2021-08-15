using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public abstract class Processor : Inbox<object>, IStartable
    {
        public TimeCounter Timer { get; private set; }
        public HeartbeatGenerator Heart { get; set; }
        public bool IsStarted => Timer.IsStarted;

        public abstract void OnTask(object value);
        public abstract void OnHeartbeat(Heartbeat heartbeat);
        public abstract void OnExceptionOccured(Exception ex);
        public abstract void OnStarted();
        public abstract void OnStopped();
        public abstract void OnFinished();

        public Processor() : base(new EventLoopScheduler())
        {
            OnReceive.Subscribe(OnTaskReceived);

            Timer = new TimeCounter();
        }

        public void EnableHeartbeat(TimeSpan interval, bool syncScheduler)
        {
            if (Heart == null)
            {
                Heart = new HeartbeatGenerator(interval, syncScheduler ? Scheduler : new EventLoopScheduler());
                Heart.OnReceive.Subscribe(OnHeartbeat);

                if (IsStarted)
                    Heart.Start();
            }
        }

        public void DisableHeartbeat()
        {
            Heart?.Dispose();
            Heart = null;
        }

        private void OnTaskReceived(object value)
        {
            try
            {
                OnTask(value);

                if (!IsStarted && NumberOfPendingTasks == 0)
                    OnFinished();
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
                Heart?.Start();
                Timer?.Start();

                OnStarted();
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                Heart?.Stop();
                Timer?.Stop();

                OnStopped();
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

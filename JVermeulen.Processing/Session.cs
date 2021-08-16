using System;
using System.Reactive.Concurrency;

namespace JVermeulen.Processing
{
    public class Session : ISession
    {
        public DateTime StartedAt { get; set; }
        public DateTime StoppedAt { get; set; }
        public TimeSpan Duration => StartedAt == default ? default : (StoppedAt == default ? DateTime.Now - StartedAt : StoppedAt - StartedAt);

        public SessionStatus Status { get; private set; }
        public SubscriptionQueue<SessionStatus> StatusChangedQueue { get; private set; }

        protected IScheduler Scheduler { get; set; }

        public Session(IScheduler scheduler = null)
        {
            Scheduler = scheduler ?? new EventLoopScheduler();

            StatusChangedQueue = new SubscriptionQueue<SessionStatus>(Scheduler);
        }

        public void SetStatus(SessionStatus status)
        {
            Status = status;
            StatusChangedQueue.Enqueue(status);
        }

        public void Start()
        {
            if (Status == SessionStatus.Stopped)
            {
                SetStatus(SessionStatus.Starting);
                OnStarting();

                StartedAt = DateTime.Now;
                StoppedAt = default;

                SetStatus(SessionStatus.Started);
                OnStarted();
            }
        }

        public void Stop()
        {
            if (Status == SessionStatus.Started)
            {
                SetStatus(SessionStatus.Stopping);
                OnStopping();

                StoppedAt = DateTime.Now;

                SetStatus(SessionStatus.Stopped);
                OnStopped();
            }
        }

        public virtual void OnStarting() { }
        public virtual void OnStarted() { }
        public virtual void OnStopping() { }
        public virtual void OnStopped() { }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

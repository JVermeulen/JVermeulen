using System;

namespace JVermeulen.Processing
{
    public class Session : ISession
    {
        public Guid Id { get; private set; }

        public SessionStatus Status { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime StoppedAt { get; private set; }
        public TimeSpan Duration => StartedAt == default ? default : (StoppedAt == default ? DateTime.Now - StartedAt : StoppedAt - StartedAt);

        public Session()
        {
            Id = Guid.NewGuid();
        }

        public void Start()
        {
            if (Status == SessionStatus.Stopped)
            {
                Status = SessionStatus.Starting;
                OnStarting();

                StartedAt = DateTime.Now;
                StoppedAt = default;

                Status = SessionStatus.Started;
                OnStarted();
            }
        }

        public void Stop()
        {
            if (Status == SessionStatus.Started)
            {
                Status = SessionStatus.Stopping;
                OnStopping();

                StoppedAt = DateTime.Now;

                Status = SessionStatus.Stopped;
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

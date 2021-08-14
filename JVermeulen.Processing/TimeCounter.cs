using System;

namespace JVermeulen.Processing
{
    public class TimeCounter : IStartStop, IDisposable
    {
        public DateTime StartedAt { get; set; }
        public DateTime StoppedAt { get; set; }
        public TimeSpan Duration => StartedAt == default ? default : (StoppedAt == default ? DateTime.Now - StartedAt : StoppedAt - StartedAt);

        public bool IsStarted => StartedAt != default && StoppedAt == default;

        public TimeCounter()
        {
            //
        }

        public virtual void Start()
        {
            StartedAt = DateTime.Now;
            StoppedAt = default;
        }

        public virtual void Stop()
        {
            StoppedAt = DateTime.Now;
        }

        public virtual void Restart()
        {
            if (IsStarted)
                Stop();

            Start();
        }

        public virtual void Dispose()
        {
            if (IsStarted)
                Stop();
        }
    }
}

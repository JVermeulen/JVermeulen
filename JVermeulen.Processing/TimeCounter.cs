using System;

namespace JVermeulen.Processing
{
    public class TimeCounter : IStartable
    {
        public DateTime StartedAt { get; set; }
        public DateTime StoppedAt { get; set; }
        public TimeSpan Duration => StartedAt == default ? default : (StoppedAt == default ? DateTime.Now - StartedAt : StoppedAt - StartedAt);

        public bool IsStarted => StartedAt != default && StoppedAt == default;

        public TimeCounter()
        {
            //
        }

        public void Start()
        {
            StartedAt = DateTime.Now;
            StoppedAt = default;
        }

        public void Stop()
        {
            StoppedAt = DateTime.Now;
        }

        public void Restart()
        {
            if (IsStarted)
                Stop();

            Start();
        }

        public void Dispose()
        {
            if (IsStarted)
                Stop();
        }
    }
}

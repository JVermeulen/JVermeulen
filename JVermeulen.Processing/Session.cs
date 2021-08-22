using System;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A startable and stoppable session with meta info.
    /// </summary>
    public class Session : ISession
    {
        /// <summary>
        /// A unique Id for this session.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The current status of this session.
        /// </summary>
        public SessionStatus Status { get; private set; }

        /// <summary>
        /// The timestamp this session started.
        /// </summary>
        public DateTime StartedAt { get; private set; }

        /// <summary>
        /// The timestamp this session stopped.
        /// </summary>
        public DateTime StoppedAt { get; private set; }

        /// <summary>
        /// The duration between StartedAt and StoppedAt. If this session is not stopped, the current time will be used.
        /// </summary>
        public TimeSpan Elapsed => StartedAt == default ? default : (StoppedAt == default ? DateTime.Now - StartedAt : StoppedAt - StartedAt);

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        public Session()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Starts the session.
        /// </summary>
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

        /// <summary>
        /// Stops the session.
        /// </summary>
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

        /// <summary>
        /// Invoked at the beginning of the Start.
        /// </summary>
        protected virtual void OnStarting() { }

        /// <summary>
        /// Invoked at the end of the Start.
        /// </summary>
        protected virtual void OnStarted() { }

        /// <summary>
        /// Invoked at the beginning of the Stop.
        /// </summary>
        protected virtual void OnStopping() { }

        /// <summary>
        /// Invoked at the end of the Stop.
        /// </summary>
        protected virtual void OnStopped() { }

        /// <summary>
        /// Stops and starts the session.
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}

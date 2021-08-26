using System;
using System.Threading;

namespace JVermeulen.Processing
{
    /// <summary>
    /// A startable and stoppable session with meta info.
    /// </summary>
    public class Session : ISession, IEquatable<Session>
    {
        /// <summary>
        /// A global unique Id.
        /// </summary>
        private static long GlobalId;

        /// <summary>
        /// A unique Id for this session.
        /// </summary>
        public long Id { get; private set; }

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
            Id = Interlocked.Increment(ref GlobalId);
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
        /// Returns true when the given object is same as this object.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        public override bool Equals(object obj)
        {
            return Equals(obj as Session);
        }

        /// <summary>
        /// Returns true when the given object is same as this object.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        public bool Equals(Session obj)
        {
            return Id == obj.Id;
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public virtual void Dispose()
        {
            Stop();
        }
    }
}

using System;

namespace JVermeulen.Processing
{
    public interface ISession : IDisposable
    {
        void Start();
        void Stop();
        void Restart();

        void OnStarting();
        void OnStarted();
        void OnStopping();
        void OnStopped();
    }
}

using System;

namespace JVermeulen.Processing
{
    public interface IStartable : IDisposable
    {
        void Start();
        void Stop();
        void Restart();
    }
}

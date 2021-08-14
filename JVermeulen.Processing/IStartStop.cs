using System;

namespace JVermeulen.Processing
{
    public interface IStartStop
    {
        void Start();
        void Stop();
        void Restart();
    }
}

using System;

namespace JVermeulen.TCP
{
    public class TcpPoll
    {
        public bool IsActive { get; private set; }

        public TcpPoll(bool isActive)
        {
            IsActive = isActive;
        }

        public override string ToString()
        {
            if (IsActive)
                return "Poll (active)";
            else
                return "Poll (inactive)";
        }
    }
}

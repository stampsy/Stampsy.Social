using System;

namespace Stampsy.Social
{
    public interface INetworkMonitor
    {
        bool IsNetworkAvailable { get; }
    }
}


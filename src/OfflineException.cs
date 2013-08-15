using System;

namespace Stampsy.Social
{
    public class OfflineException : Exception
    {
        public OfflineException ()
            : base ("The internet connection appears to be offline")
        {
        }
    }
}


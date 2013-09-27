using System;
#if PLATFORM_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#elif PLATFORM_ANDROID
using Android.OS;
#endif


namespace Stampsy.Social
{
    internal static class Utils
    {
        public static void EnsureMainThread ()
        {
#if PLATFORM_IOS
            UIApplication.EnsureUIThread ();
#elif PLATFORM_ANDROID
            if (Looper.MyLooper () != Looper.MainLooper)
                throw new InvalidOperationException ("Trying to run UI code from non-ui thread.");
#else
            throw new NotImplementedException ("EnsureMainThread is not implemented on this platform.");
#endif
        }
    }
}
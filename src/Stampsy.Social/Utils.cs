#if PLATFORM_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#elif PLATFORM_ANDROID

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

#else

#endif
        }
    }
}
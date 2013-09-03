using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Stampsy.Social
{
    internal static class Utils
    {
        public static void EnsureMainThread ()
        {
            UIApplication.EnsureUIThread ();
        }
    }
}
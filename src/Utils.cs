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

        public static void CheckNotNull (object o, string name)
        {
            if (o == null)
                throw new ArgumentNullException (name);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Stampsy.Social;
using Xamarin.Auth;

namespace Sociopath
{
    public static class Utils
    {
        [Conditional ("DEBUG")]
        public static void EnsureMainThread ()
        {
            if (!NSThread.IsMain)
                throw new InvalidOperationException ("Changing service state is only allowed on main thread. Eat it!");
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Sociopath
{
    [Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            TaskScheduler.UnobservedTaskException += (sender, e) => {
                Console.WriteLine (e.Exception);
                e.SetObserved ();
            };

            return true;
        }

        public override bool OpenUrl (UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            return SafariUrlHandler.Instance.HandleOpenUrl (new Uri (url.AbsoluteString));
        }

        public override void WillEnterForeground (UIApplication application)
        {
            SafariUrlHandler.Instance.WillEnterForeground ();
        }
    }
}


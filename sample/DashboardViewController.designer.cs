// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Sociopath
{
	[Register ("DashboardViewController")]
	partial class DashboardViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton dropboxButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton facebookButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton twitterButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton googleButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (dropboxButton != null) {
				dropboxButton.Dispose ();
				dropboxButton = null;
			}

			if (facebookButton != null) {
				facebookButton.Dispose ();
				facebookButton = null;
			}

			if (twitterButton != null) {
				twitterButton.Dispose ();
				twitterButton = null;
			}

			if (googleButton != null) {
				googleButton.Dispose ();
				googleButton = null;
			}
		}
	}
}

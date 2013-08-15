// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Sociopath
{
	[Register ("GoogleViewController")]
	partial class GoogleViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIButton loginButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIActivityIndicatorView loadingIndicator { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITableView tableView { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel statusLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (loginButton != null) {
				loginButton.Dispose ();
				loginButton = null;
			}

			if (loadingIndicator != null) {
				loadingIndicator.Dispose ();
				loadingIndicator = null;
			}

			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}

			if (statusLabel != null) {
				statusLabel.Dispose ();
				statusLabel = null;
			}
		}
	}
}

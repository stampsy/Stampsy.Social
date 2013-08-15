// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;
using System.CodeDom.Compiler;

namespace Sociopath
{
	[Register ("FacebookViewController")]
	partial class FacebookViewController
	{
		[Outlet]
		MonoTouch.UIKit.UIActivityIndicatorView loadingIndicator { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton loginButton { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel statusLabel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITableView tableView { get; set; }

		[Action ("UIButton15_TouchUpInside:")]
		partial void HandleShare (MonoTouch.UIKit.UIButton sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}

			if (loginButton != null) {
				loginButton.Dispose ();
				loginButton = null;
			}

			if (statusLabel != null) {
				statusLabel.Dispose ();
				statusLabel = null;
			}

			if (loadingIndicator != null) {
				loadingIndicator.Dispose ();
				loadingIndicator = null;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Auth;
using Xamarin.Social;
using Xamarin.Social.Services;
using Stampsy.Social;
using Stampsy.Social.Services;

namespace Sociopath
{
	public partial class GoogleViewController : ServiceViewController
	{
        protected override ServiceManager Service {
            get { return Services.Google; }
        }

        protected override UITableView TableView {
            get { return tableView; }
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            loginButton.TouchUpInside += (sender, e) =>
                GetProfileAndFriendsAsync (LoginOptions.WithUI);
        }

        protected override void OnStateChanged (SessionState state)
        {
            if (state == SessionState.Authenticating)
                loadingIndicator.StartAnimating ();
            else
                loadingIndicator.StopAnimating ();

            statusLabel.Text = state.ToString ();
        }

		public GoogleViewController (IntPtr handle) : base (handle)
		{
		}
	}
}

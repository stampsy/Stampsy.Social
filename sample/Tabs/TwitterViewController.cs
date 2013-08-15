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
	public partial class TwitterViewController : ServiceViewController
	{
        protected override UITableView TableView {
            get { return tableView; }
        }

        protected override ServiceManager Service {
            get { return Services.Twitter; }
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            loginButton.TouchUpInside += (sender, e) =>
                GetProfileAndFriendsAsync (
                    LoginOptions.WithUIAndChoice (new ActionSheetChoiceProvider<Account> (loginButton.Frame, View))
                );
        }

        protected override void OnStateChanged (ServiceState state)
        {
            if (state == ServiceState.Authenticating)
                loadingIndicator.StartAnimating ();
            else
                loadingIndicator.StopAnimating ();

            statusLabel.Text = state.ToString ();
        }

        async partial void HandleShare (NSObject sender)
        {
            try {
                await Services.Twitter.ShareAsync (new Item () {
                    Text = "DAFT PUNK ARE STANDING ON A HELIPAD " + DateTime.Now.ToString (), /*overlooking downtown Los Angeles as fireballs make their sequined suits glisten with hot heat. It's a few days before this year's Coachella, where the duo's shiny new duds will premiere by way of a Jumbotron trailer for their new album, Random Access Memories. But for now, only a very select few have laid eyes on the outfits—and everyone involved in today's photo shoot desperately wants to keep it that way.",*/
                    Links = new List<Uri> { new Uri ("http://pitchfork.com/features/cover-story/reader/daft-punk/") }
                }, options: LoginOptions.WithUI);

                new UIAlertView ("OK", "OK", null, "OK").Show ();
            } catch (Exception ex) {
                new UIAlertView ("Share Failed", ex.ToString (), null, "OK").Show ();
            }
        }

		public TwitterViewController (IntPtr handle) : base (handle)
		{
		}
	}
}

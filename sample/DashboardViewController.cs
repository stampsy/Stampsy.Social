using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Auth;
using Stampsy.Social;
using Xamarin.Social.Services;
using Xamarin.Social;

namespace Sociopath
{
	public partial class DashboardViewController : UIViewController
	{
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            TryLoginAndWatch (facebookButton, Services.Facebook);
            TryLoginAndWatch (twitterButton, Services.Twitter);
            TryLoginAndWatch (googleButton, Services.Google);
            TryLoginAndWatch (dropboxButton, Services.Dropbox, PresentViewController);
        }

        void TryLoginAndWatch (UIButton button, ServiceManager service, Action<UIViewController, bool, NSAction> presentAuthController = null)
        {
            UpdateLabel (button, service);
            service.StateChanged += (sender, e) =>
                UpdateLabel (button, service);

            button.TouchUpInside += (sender, e) => {
                switch (service.State) {
                case SessionState.LoggedIn:
                case SessionState.Authenticating:
                    service.CloseSession ();
                    service.DeleteStoredAccounts ();
                    break;
                case SessionState.LoggedOut:
                    var choiceUI = new ActionSheetChoiceProvider<Account> (button.Frame, View);
                    service.GetSessionAsync (LoginOptions.WithUIAndChoice (choiceUI, presentAuthController: presentAuthController));
                    break;
                }
            };

            try {
                service.GetSessionAsync (LoginOptions.NoUI);
            } catch (UriFormatException) {
                Console.WriteLine ("Open Services.cs and specify your API keys for each provider.");
                return;
            }
        }

        void UpdateLabel (UIButton button, ServiceManager service)
        {
            var state = service.State;

            switch (state) {
            case SessionState.LoggedIn:
                var acc = service.ActiveSession.Account;
                button.SetTitle (string.Format ("Log out from {0}", acc.Username), UIControlState.Normal);
                break;

            case SessionState.Authenticating:
                button.SetTitle ("Logging in", UIControlState.Normal);
                break;

            case SessionState.LoggedOut:
                button.SetTitle ("Log in", UIControlState.Normal);
                break;
            }
        }

		public DashboardViewController (IntPtr handle) : base (handle)
		{
		}
	}
}

using System;
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Stampsy.Social;

namespace Sociopath
{
    public abstract class ServiceViewController : UIViewController
    {
        protected abstract ServiceManager Service { get; }
        protected abstract UITableView TableView { get; }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            OnStateChanged (Service.State);
            Service.StateChanged += (sender, e) =>
                OnStateChanged (Service.State);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            if (TableView.Source == null)
                GetProfileAndFriendsAsync (LoginOptions.NoUI);
        }

        protected virtual void OnStateChanged (SessionState state)
        {
        }

        protected virtual async void GetProfileAndFriendsAsync (LoginOptions options)
        {
            TableView.Source = null;
            TableView.ReloadData ();

            try {
                var profile = await Service.GetProfileAsync (options);
                new UIAlertView ("Profile", profile.ToString (), null, "OK").Show ();
            } catch (UriFormatException) {
                new UIAlertView ("Specify your API keys", "Open Services.cs and specify your API keys for each provider.", null, "OK").Show ();
                return;
            } catch {
                Console.WriteLine ("Couldn't retrieve profile for {0}. (Maybe we're logged out?)", Service);
                return;
            }

            GetNextFriendsAsync (options);
        }

        protected virtual async void GetNextFriendsAsync (LoginOptions options, Page<IEnumerable<ServiceUser>> previous = null)
        {
            Page<IEnumerable<ServiceUser>> friendsPage;

            try {
                friendsPage = await Service.GetFriendsAsync (previous: previous, options: options);
            } catch {
                Console.WriteLine ("Failed to retrieve friends for {0}. (Maybe we're logged out?)", Service);
                return;
            }

            var friends = friendsPage.Value;

            if (TableView.Source == null) {
                TableView.Source = new ServiceUserSource (friends);
            } else {
                ((ServiceUserSource) TableView.Source).Add (friends);
            }

            TableView.ReloadData ();

            if (friendsPage.HasNextPage) {
                NSTimer.CreateScheduledTimer (1, () => {
                    GetNextFriendsAsync (options, friendsPage);
                });
            }
        }

        public ServiceViewController (IntPtr handle)
            : base (handle)
        {
        }
    }
}


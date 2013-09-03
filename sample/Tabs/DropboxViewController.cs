using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Stampsy.Social;

namespace Sociopath
{
	public partial class DropboxViewController : UIViewController
	{
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            getRootButton.TouchUpInside += (sender, e) =>
                GetRootFolder (LoginOptions.WithUIAndChoice (presentAuthController: PresentViewController));

            OnStateChanged (Services.Dropbox.State);
            Services.Dropbox.StateChanged += (sender, e) =>
                OnStateChanged (Services.Dropbox.State);
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            if (tableView.Source == null)
                GetRootFolder (LoginOptions.NoUI);
        }

        async void GetRootFolder (LoginOptions options)
        {
            tableView.Source = null;
            tableView.ReloadData ();

            try {
                var profile = await Services.Dropbox.GetProfileAsync (options: options);
                new UIAlertView ("Profile", profile.ToString (), null, "OK").Show ();
            } catch (UriFormatException) {
                new UIAlertView ("Specify your API keys", "Open Services.cs and specify your API keys for each provider.", null, "OK").Show ();
                return;
            } catch {
                Console.WriteLine ("Failed to retrieve Dropbox profile. (Maybe we're logged out?)");
                return;
            }

            DropboxManager.Metadata metadata;
            try {
                metadata = await Services.Dropbox.GetMetadataAsync ("/", includeContent: true, hash: null, options: options);
            } catch {
                Console.WriteLine ("Failed to retrieve Dropbox metadata");
                return;
            }

            if (tableView.Source == null) {
                var source = new MetadataSource (metadata.Contents) {
                    Controller = new WeakReference (this)
                };
                tableView.Source = source;
            } else {
                ((MetadataSource)tableView.Source).Add (metadata.Contents);
            }

            tableView.ReloadData ();
        }

        void OnStateChanged (SessionState state)
        {
            if (state == SessionState.Authenticating)
                loadingIndicator.StartAnimating ();
            else
                loadingIndicator.StopAnimating ();

            statusLabel.Text = state.ToString ();
        }

		public DropboxViewController (IntPtr handle) : base (handle)
		{
		}

        public void ShowImage (string destPath)
        {
            BeginInvokeOnMainThread (() => {
                var imageController = new ImageViewController (destPath);

                var navController = new UINavigationController (imageController);
                navController.ModalPresentationStyle = UIModalPresentationStyle.FormSheet;

                PresentViewController (navController, true, () => {});
            });
        }

        public class ImageViewController : UIViewController
        {
            string _path;

            public ImageViewController (string path) : base ()
            {
                _path = path;
            }

            public override void LoadView ()
            {
                base.LoadView ();

                View.BackgroundColor = UIColor.Black;

                NavigationItem.RightBarButtonItem = new UIBarButtonItem ("Close", UIBarButtonItemStyle.Plain, (s, e) => {
                    DismissViewController (true, () => {});
                });

                var image = UIImage.FromFile (_path);
                var imageView = new UIImageView (image) {
                    Frame = View.Bounds,
                    AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
                    ContentMode = UIViewContentMode.ScaleAspectFit
                };

                View.AddSubview (imageView);
            }
        }
	}
}

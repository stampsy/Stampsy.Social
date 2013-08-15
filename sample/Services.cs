using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MonoTouch.Foundation;
using Xamarin.Social.Services;
using Xamarin.Social;
using Xamarin.Auth;
using Stampsy.Social;
using Stampsy.Social.Services;

namespace Sociopath
{
    public static class Services
    {
        public static FacebookManager Facebook {
            get { return _facebook.Value; }
        }

        public static TwitterManager Twitter {
            get { return _twitter.Value; }
        }

        public static GoogleManager Google {
            get { return _google.Value; }
        }

        public static DropboxManager Dropbox {
            get { return _dropbox.Value; }
        }

        public static readonly Lazy<FacebookManager> _facebook = new Lazy<FacebookManager> (
            () => new FacebookManager (
                () => new Facebook6Service {
                    FacebookAppId = "YOUR_APP_ID",
                    Scope = "email"
                },
                () => new FacebookService {
                    ClientId = "YOUR_APP_ID",
                    RedirectUrl = new Uri ("fbYOUR_APP_ID://authorize"),
                    Scope = "email"
                }
            )
        );

        public static readonly Lazy<TwitterManager> _twitter = new Lazy<TwitterManager> (
            () => new TwitterManager (
                () => new Twitter6Service (),
                () => new TwitterService {
                    ConsumerKey = "YOUR_CONSUMER_KEY", 
                    ConsumerSecret = "YOUR_CONSUMER_SECRET",
                    CallbackUrl = new Uri ("YOUR_CALLBACK_SCHEME://connect")
                }
            )
        );

        public static readonly Lazy<GoogleManager> _google = new Lazy<GoogleManager> (
            () => new GoogleManager (
                () => new GoogleService {
                    ClientId = "YOUR_CLIENT_ID.apps.googleusercontent.com",
                    ClientSecret = "YOUR_CLIENT_SECRET",
                    RedirectUrl = new Uri ("YOUR_BUNDLE_ID:/oauth2callback"),
                    Scope = "https://www.googleapis.com/auth/plus.me"
                }
            )
        );

        public static readonly Lazy<DropboxManager> _dropbox = new Lazy<DropboxManager> (
            () => new DropboxManager (
                () => new DropboxService {
                    ConsumerKey = "YOUR_CONSUMER_KEY",
                    ConsumerSecret = "YOUR_CONSUMER_SECRET",
                    CallbackUrl = new Uri ("YOUR_CALLBACK_SCHEME://1/connect"),
                    Root = "sandbox"
                }
            )
        );
    }
}


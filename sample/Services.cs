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
        public static readonly FacebookManager Facebook = new FacebookManager (
            () => new Facebook6Service {
                FacebookAppId = "YOUR_APP_ID",
                Scope = "email"
            },
            () => new FacebookService {
                ClientId = "YOUR_APP_ID",
                RedirectUrl = new Uri ("fbYOUR_APP_ID://authorize"),
                Scope = "email"
            }
        );

        public static readonly TwitterManager Twitter = new TwitterManager (
            () => new Twitter6Service (),
            () => new TwitterService {
                ConsumerKey = "YOUR_CONSUMER_KEY", 
                ConsumerSecret = "YOUR_CONSUMER_SECRET",
                CallbackUrl = new Uri ("YOUR_CALLBACK_SCHEME://connect")
            }
        );

        public static readonly GoogleManager Google = new GoogleManager (
            () => new GoogleService {
                ClientId = "YOUR_CLIENT_ID.apps.googleusercontent.com",
                ClientSecret = "YOUR_CLIENT_SECRET",
                RedirectUrl = new Uri ("YOUR_BUNDLE_ID:/oauth2callback"),
                Scope = "https://www.googleapis.com/auth/plus.me"
            }
        );

        public static readonly DropboxManager Dropbox = new DropboxManager (
            () => new DropboxService {
                ConsumerKey = "YOUR_CONSUMER_KEY",
                ConsumerSecret = "YOUR_CONSUMER_SECRET",
                CallbackUrl = new Uri ("YOUR_CALLBACK_SCHEME://1/connect"),
                Root = "sandbox"
            }
        );
    }
}


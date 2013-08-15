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
                    FacebookAppId = "419123104792477",
                    Scope = "email"
                },
                () => new FacebookService {
                    ClientId = "419123104792477",
                    RedirectUrl = new Uri ("fb419123104792477://authorize"),
                    Scope = "email"
                }
            )
        );

        public static readonly Lazy<TwitterManager> _twitter = new Lazy<TwitterManager> (
            () => new TwitterManager (
                () => new Twitter6Service (),
                () => new TwitterService {
                    ConsumerKey = "HXysWWOpI3TdEwdNkrJneQ", 
                    ConsumerSecret = "D8SJNHbWcC6msiLXvHkwm6sJBo3UzQF9RYTKnD1v7jk", 
                    CallbackUrl = new Uri ("tw-stampsy://connect")
                }
            )
        );

        public static readonly Lazy<GoogleManager> _google = new Lazy<GoogleManager> (
            () => new GoogleManager (
                () => new GoogleService {
                    ClientId = "1032245683715.apps.googleusercontent.com",
                    ClientSecret = "bieX5mvbe3M002T4uglDriRu",
                    RedirectUrl = new Uri ("com.stampsy.ipad:/oauth2callback"),
                    Scope = "https://www.googleapis.com/auth/plus.me"
                }
            )
        );

        public static readonly Lazy<DropboxManager> _dropbox = new Lazy<DropboxManager> (
            () => new DropboxManager (
                () => new DropboxService {
                    ConsumerKey = "r9fwzeogctrcr0r",
                    ConsumerSecret = "tgb6kaudi1ul5pl",
                    CallbackUrl = new Uri ("db-r9fwzeogctrcr0r://1/connect"),
                    Root = "sandbox"
                }
            )
        );
    }

}


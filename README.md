Stampsy.Social
==============

A library on top of Xamarin.Auth and Xamarin.Social that provides native login with Safari fallback, and implements common APIs for Facebook, Twitter, Google and Dropbox.

### Dependencies

* [Newtonsoft.Json](https://github.com/ayoung/Newtonsoft.Json/)
* [Xamarin.Auth fork](http://github.com/stampsy/Xamarin.Auth/)
* [Xamarin.Social fork](https://github.com/stampsy/Xamarin.Social/)

(We need forks because there's some too iOS-specific stuff in there. There is an ongoing effort on porting some of it to Android. I try to keep these forks in sync with Xamarin's versions.)

### Why

Our goal was to have a unified interface to authenticating and calling Facebook, Twitter and Google APIs. *Managers*, such as `FacebookManager` and `TwitterManager` wrap Xamarin.Social services, but allow to specify several services for fallback. For example, we might want to first use native login, and if it fails, fall back to Safari:

```c#
static class Services {
    public static readonly FacebookManager Facebook = new FacebookManager (
        // First, try native login
        () => new Facebook6Service {
            FacebookAppId = "YOUR_APP_ID",
            Scope = "email"
        },
        // Fall back to Safari
        () => new FacebookService {
            ClientId = "YOUR_APP_ID",
            RedirectUrl = new Uri ("fbYOUR_APP_ID://authorize"),
            Scope = "email"
        }
    );
}
```

We also wanted the consuming code to not have to think about authentication:

```c#
var profile = await Services.Facebook.GetProfileAsync (options: LoginOptions.WithUI);
Console.WriteLine (profile.Name);
```

If the user is not authenticated, this will either present native dialog, or open Safari. When (and if) user authenticates, it will then get profile using Open Graph API and parse it into a C# object.

If we didn't want to present login UI, we'd just pass `LoginOptions.NoUI`, and if the user is logged out, the call would fail silently.

We're using this in production in Stampsy 1.5 (soon to appear in App Store).

### Features

* Athentication with iOS 6 providers with fallback to Safari
* Support for choosing native Twitter with an action sheet (see sample project)
* Getting profile for Facebook, Twitter, Google+ and Dropbox
* Getting access token from Facebook, Twitter ([Reverse Auth](https://dev.twitter.com/docs/ios/using-reverse-auth)) and Google+
* Getting paginated friends for Facebook, Twitter and Google Plus
* Sharing support for Facebook, Twitter and Google+
* Downloading files and thumbnails from Dropbox
* Similarly to Facebook SDK, client code just passes a parameter whether login UI can be displayed during an API call
* All calls return `Task`s and support cancellations

Open `sample/Sociopath.sln` to see how it works.  
(Don't forget to put your API keys in `sample/Services.cs`.)

### Shortcomings

* This README is the only planned documentation
* No unit tests (yet?)

### Cloning the Repo

Just clone it recursively:

    git clone https://github.com/stampsy/Stampsy.Social.git --recursive

This will fetch Stampsy.Social, as well as our forks of Xamarin.Auth and Xamarin.Social.  
Then **fill in your API keys** in `sample/Services.cs`.

Have fun!

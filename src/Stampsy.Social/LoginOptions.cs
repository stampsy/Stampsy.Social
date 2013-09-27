using System;
using Xamarin.Auth;
#if PLATFORM_IOS
using MonoTouch.UIKit;
using MonoTouch.Foundation;
#elif PLATFORM_ANDROID
using Android.App;
using Android.Content;
#endif

namespace Stampsy.Social
{
    public struct LoginOptions
    {
        
        public static readonly LoginOptions NoUI = new LoginOptions { AllowLoginUI = false };
#if PLATFORM_IOS
        public static readonly LoginOptions WithUI = new LoginOptions { AllowLoginUI = true };
        public Action<UIViewController, bool, NSAction> PresentAuthController { get; set; }

        public static LoginOptions WithUIAndChoice (
            IChoiceProvider<Account> accountChoiceProvider = null,
            Action<LoginProgress> reportProgress = null,
            Action<UIViewController, bool, NSAction> presentAuthController = null)
        {
            return new LoginOptions {
                AllowLoginUI = true,
                AccountChoiceProvider = accountChoiceProvider,
                PresentAuthController = presentAuthController,
                ReportProgress = reportProgress
            };
        }
#elif PLATFORM_ANDROID
        public Action<Activity, Intent, bool, Action> PresentAuthController { get; set; }
        public Activity Activity;

        public static LoginOptions WithUI (Activity activity)
        {
            return new LoginOptions {
                AllowLoginUI = true,
                Activity = activity
            };
        }
#endif

        public bool AllowLoginUI { get; set; }
        public IChoiceProvider<Account> AccountChoiceProvider { get; set; }
        public Action<LoginProgress> ReportProgress { get; set; }

        private LoginProgress? _lastProgress;

        internal void TryReportProgress (LoginProgress progress)
        {
            if (ReportProgress != null && progress != _lastProgress) {
                ReportProgress (progress);
                _lastProgress = progress;
            }
        }
    }
}
using System;
using Xamarin.Auth;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace Stampsy.Social
{
    public struct LoginOptions
    {
        public static readonly LoginOptions WithUI = new LoginOptions { AllowLoginUI = true };
        public static readonly LoginOptions NoUI = new LoginOptions { AllowLoginUI = false };

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

        public bool AllowLoginUI { get; set; }
        public IChoiceProvider<Account> AccountChoiceProvider { get; set; }
        public Action<LoginProgress> ReportProgress { get; set; }
        public Action<UIViewController, bool, NSAction> PresentAuthController { get; set; }

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
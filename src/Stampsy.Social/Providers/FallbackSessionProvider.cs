using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Social;
using Service = Xamarin.Social.Service;

#if PLATFORM_IOS
using MonoTouch.UIKit;
using MonoTouch.Foundation;
#elif PLATFORM_ANDROID
using Android.App;
#endif

namespace Stampsy.Social.Providers
{
    internal class FallbackSessionProvider : ISessionProvider
    {
        private readonly IEnumerable<Func<Service>> _fallbackChain;

        public FallbackSessionProvider (params Func<Service> [] fallbackChain)
        {
            if (fallbackChain.Length == 0)
                throw new ArgumentOutOfRangeException ("fallbackChain", "Fallback chain is empty.");

            _fallbackChain = fallbackChain;
        }

        public async Task<Session> Login (LoginOptions options, string [] scope, CancellationToken token)
        {
            List<AccountProvider> providers = GetProviderChain (options, scope).ToList ();
            options.TryReportProgress (LoginProgress.Authorizing);

            var providerExceptions = new List<Exception> ();

            // Try each provider in turn
            foreach (var pi in providers.Select ((p, i) => new { Provider = p, Index = i })) {
                bool isLast = (pi.Index == providers.Count - 1);
                AccountProvider provider = pi.Provider;

                token.ThrowIfCancellationRequested ();

                try {
                    return await GetSession (provider, isLast, options, token);
                } catch (TaskCanceledException) {
                    throw;
                } catch (Exception ex) {
                    providerExceptions.Add (ex);
                }
            }

            // Neither provider worked
            throw new AggregateException ("Could not obtain session via either provider.", providerExceptions);
        }

        async Task<Session> GetSession (AccountProvider provider, bool isLast, LoginOptions options, CancellationToken token)
        {
            if (!SessionManager.NetworkMonitor.IsNetworkAvailable)
                throw new OfflineException ();

            var account = await GetAccount (provider, !isLast, options);
            if (account == null)
                throw new Exception ("The user chose to skip this provider.");

            var service = provider.Service;
            var session = new Session (service, account);

            if (service.SupportsVerification) {
                // For services that support verification, do it now
                try {
                    await service.VerifyAsync (account, token);
                } catch (TaskCanceledException) {
                    throw;
                } catch (Exception ex) {
                    throw new InvalidOperationException ("Account verification failed.", ex);
                }
            }

            return session;
        }

        async Task<Account> GetAccount (AccountProvider provider, bool allowFallback, LoginOptions options)
        {
            List<Account> accounts;

            options.TryReportProgress (provider.ProgressWhileAuthenticating);
            try {
                // Now, let's get accounts for current provider.
                // For different services and login methods, this may launch Safari, show iOS 6 prompt or just query ACAccounts.
                accounts = (await provider.GetAccounts ()).ToList ();
            } finally {
                options.TryReportProgress (LoginProgress.Authorizing);
            }

            if (accounts.Count == 0)
                throw new InvalidOperationException ("No accounts found for this service.");

            if (accounts.Count == 1)
                return accounts [0];

            // If there is more than a single account, present an interface to choose one.
            // If fallback is available, add it to the list of options with null value.

            var choiceUI = options.AccountChoiceProvider;
            if (choiceUI == null)
                throw new InvalidOperationException ("There is more than one account, but no accountChoiceProvider was specified.");

            // Add "Other" option that falls back to next provider
            if (allowFallback)
                accounts.Add (null);

            // Show chooser interface
            options.TryReportProgress (LoginProgress.PresentingAccountChoice);
            try {
                return await choiceUI.ChooseAsync (accounts, a => (a != null) ? a.Username : "Other");
            } finally {
                options.TryReportProgress (LoginProgress.Authorizing);
            }
        }

#if PLATFORM_IOS
        IEnumerable<AccountProvider> GetProviderChain (LoginOptions options, string [] scope)
        {
            foreach (var serviceFactory in _fallbackChain) {
                var service = serviceFactory ();

                var scoped = service as ISupportScope;
                if (scoped != null && scope != null)
                    scoped.Scopes = scope;

                var system = service as IOSService;
                if (system != null)
                    system.AllowLoginUI = options.AllowLoginUI;

                yield return new AccountProvider (service, false);

                if (options.AllowLoginUI && service.SupportsAuthentication) {
                    if (options.PresentAuthController != null)
                        yield return new AccountProvider (service, options.PresentAuthController);
                    else
                        yield return new AccountProvider (service, true);
                }
            }
        }
#elif PLATFORM_ANDROID
        IEnumerable<AccountProvider> GetProviderChain (LoginOptions options, string[] scope)
        {
            foreach (var serviceFactory in _fallbackChain) {
                var service = serviceFactory ();

                var scoped = service as ISupportScope;
                if (scoped != null && scope != null)
                    scoped.Scopes = scope;

                yield return new AccountProvider (service, null);

                if (options.AllowLoginUI && service.SupportsAuthentication) {
                    if (options.Activity != null)
                        yield return new AccountProvider (service, options.Activity);
                }
            }
        }
#else
        IEnumerable<AccountProvider> GetProviderChain (LoginOptions options, string [] scope)
        {
            throw new NotImplementedException ("GetProviderChain is not implemented on this platform.");
        }
#endif

#if PLATFORM_IOS
        class AccountProvider
        {
            public Service Service { get; private set; }
            public bool UseSafari { get; private set; }
            public Action<UIViewController, bool, NSAction> PresentAuthController { get; private set; }

            public LoginProgress ProgressWhileAuthenticating {
                get {
                    if (PresentAuthController != null)
                        return LoginProgress.PresentingAuthController;

                    if (UseSafari)
                        return LoginProgress.PresentingBrowser;

                    return LoginProgress.Authorizing;
                }
            }

            public AccountProvider (Service service, Action<UIViewController, bool, NSAction> presentAuthController)
            {
                if (!service.SupportsAuthentication)
                    throw new NotSupportedException (string.Format ("{0} does not support authentication with a controller.", service.Title));

                Service = service;
                PresentAuthController = presentAuthController;
            }

            public AccountProvider (Service service, bool useSafari)
            {
                if (useSafari && !service.SupportsAuthentication)
                    throw new NotSupportedException (string.Format ("{0} does not support authentication with Safari.", service.Title));

                Service = service;
                UseSafari = useSafari;
            }

            public Task<IEnumerable<Account>> GetAccounts ()
            {
                if (UseSafari)
                    return Service.GetAccountsWithBrowserAsync (SafariUrlHandler.Instance);
                else if (PresentAuthController != null)
                    return Service.GetAccountsWithAuthUIAsync (PresentAuthController);
                else
                    return Service.GetAccountsAsync ();
            }
        }
#elif PLATFORM_ANDROID
        class AccountProvider
        {
            public Service Service { get; private set; }
            private Activity Activity { get; set; }

            public LoginProgress ProgressWhileAuthenticating
            {
                get {
                    if (Activity != null)
                        return LoginProgress.PresentingAuthController;

                    return LoginProgress.Authorizing;
                }
            }

            public AccountProvider (Service service, Activity activity)
            {
                if (!service.SupportsAuthentication)
                    throw new NotSupportedException (string.Format ("{0} does not support authentication with a controller.", service.Title));

                Service = service;
                Activity = activity;
            }

            public AccountProvider (Service service)
            {
                Service = service;
            }

            public Task<IEnumerable<Account>> GetAccounts ()
            {
                if (Activity != null)
                    return Service.GetAccountsWithAuthUIAsync (Activity);

                return Service.GetAccountsAsync (Activity);
            }
        }
#else
        class AccountProvider
        {
            public Service Service { get; private set; }

            public LoginProgress ProgressWhileAuthenticating {
                get {
                    throw new NotImplementedException ("ProgressWhileAuthenticating is not implemented on this platform.");
                }
            }

            public Task<IEnumerable<Account>> GetAccounts ()
            {
                throw new NotImplementedException ("GetAccounts is not implemented on this platform.");
            }
        }
#endif
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Social;
using Xamarin.Social.Services;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

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

        public async Task<Session> Login (LoginOptions options, string [] scope)
        {
            List<AccountProvider> providers = GetProviderChain (options, scope).ToList ();
            options.TryReportProgress (LoginProgress.Authorizing);

            var providerExceptions = new List<Exception> ();

            // Try each provider in turn

            foreach (var pi in providers.Select ((p, i) => new { Provider = p, Index = i })) {
                bool isLast = (pi.Index == providers.Count - 1);
                AccountProvider provider = pi.Provider;

                if (!SessionManager.NetworkMonitor.IsNetworkAvailable)
                    throw new OfflineException ();

                try {
                    List<Account> accounts = null;

                    // Now, let's get accounts for current provider.
                    // For different services and login methods, this may launch Safari, show iOS 6 prompt or just query ACAccounts.

                    options.TryReportProgress (provider.ProgressWhileAuthenticating);
                    try {
                        accounts = (await provider.GetAccounts ()).ToList ();
                    } finally {
                        options.TryReportProgress (LoginProgress.Authorizing);
                    }

                    Account account = null;

                    if (accounts.Count == 0) {
                        throw new InvalidOperationException ("No accounts found for this service.");
                    } else if (accounts.Count == 1) {
                        account = accounts [0];
                    } else {
                        // If there is more than a single account, present an interface to choose one.
                        // If fallback is available, add it to the list of options with null value.

                        var choiceUI = options.AccountChoiceProvider;
                        if (choiceUI == null)
                            throw new InvalidOperationException ("There is more than one account, but no accountChoiceProvider was specified.");

                        // Add "Other" option that will just fall back to next provider
                        if (!isLast)
                            accounts.Add (null);

                        // Show chooser interface
                        options.TryReportProgress (LoginProgress.PresentingAccountChoice);
                        try {
                            account = await choiceUI.ChooseAsync (accounts, (a) => (a != null) ? a.Username : "Other");
                        } finally {
                            options.TryReportProgress (LoginProgress.Authorizing);
                        }

                        // If the user has chosen "Other" option, fall back to next provider
                        if (account == null)
                            continue;
                    }

                    var service = provider.Service;
                    var session = new Session (service, account);

                    if (service.SupportsVerification) {
                        // For services that support verification, do it now
                        try {
                            await service.VerifyAsync (account);
                        } catch (Exception ex) {
                            throw new InvalidOperationException ("Account verification failed.", ex);
                        }
                    }

                    // OK
                    return session;

                } catch (TaskCanceledException) {
                    throw;
                } catch (Exception ex) {
                    // Whenever authorization fails, store the exception.
                    // If neither provider works, we'll throw an aggregate exception with this list.
                    providerExceptions.Add (ex);
                }
            }

            throw new AggregateException ("Could not obtain session via either provider", providerExceptions);
        }

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
                        return LoginProgress.PresentingSafari;

                    return LoginProgress.Authorizing;
                }
            }

            public AccountProvider (Service service, Action<UIViewController, bool, NSAction> presentAuthController)
            {
                if (!service.SupportsAuthentication)
                    throw new NotSupportedException (string.Format ("{0} does not support authentication with a controller", service.Title));

                Service = service;
                PresentAuthController = presentAuthController;
            }

            public AccountProvider (Service service, bool useSafari)
            {
                if (useSafari && !service.SupportsAuthentication)
                    throw new NotSupportedException (string.Format ("{0} does not support authentication with Safari", service.Title));

                Service = service;
                UseSafari = useSafari;
            }

            public Task<IEnumerable<Account>> GetAccounts ()
            {
                if (UseSafari)
                    return Service.GetAccountsAsync (SafariUrlHandler.Instance);
                else if (PresentAuthController != null)
                    return Service.GetAccountsAsync (PresentAuthController);
                else
                    return Service.GetAccountsAsync ();
            }
        }
    }
}
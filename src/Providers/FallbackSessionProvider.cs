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
            _fallbackChain = fallbackChain;
        }

        public Task<Session> Login (LoginOptions options, string [] scope = null)
        {
            var chain = GetProviderChain (options, scope);

            if (chain.Count == 0)
                throw new InvalidOperationException ("Fallback chain for has no elements.");

            options.TryReportProgress (LoginProgress.Authorizing);
            return Login (chain.First, options, scope);
        }

        Task<Session> Login (LinkedListNode<AccountProvider> currentProvider, LoginOptions options, string [] scope)
        {
            if (!SessionManager.NetworkMonitor.IsNetworkAvailable)
                return Task.Factory.FromException<Session> (new OfflineException ());

            var provider = currentProvider.Value;
            var tcs = new TaskCompletionSource<Session> ();

            // Introduce a helper to set task result from 
            // recursively calling this function with next provider:

            Func<bool> loginWithNextProvider = () => {
                var nextProvider = currentProvider.Next;
                if (nextProvider == null)
                    return false;

                Login (nextProvider, options, scope).ContinueWith (
                    tcs.SetFromTask
                );

                return true;
            };

            // Introduce a helper to complete current task with an account,
            // falling back to the next provider if verification fails:

            Action<Account> loginWithCurrentProvider = (acc) => {
                var service = provider.Service;
                var session = new Session (service, acc);

                if (!service.SupportsVerification) {
                    tcs.SetResult (session);
                    return;
                }

                var verifyTask = service.VerifyAsync (acc);

                // In case of success, return session
                verifyTask.ContinueWith (
                    t => tcs.SetResult (session),
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );

                // In case of failure, try next provider
                verifyTask.ContinueWith (t => {
                    if (!loginWithNextProvider ())
                        tcs.SetException (new Exception ("Account verification failed, and there was no fallback account."));
                }, TaskContinuationOptions.NotOnRanToCompletion);
            };


            // Now, let's get accounts for current provider.
            // For different services and login methods, this may launch Safari, show iOS 6 prompt or just query ACAccounts.

            options.TryReportProgress (provider.ProgressWhileAuthenticating);
            provider.GetAccounts ().ContinueWith (t => {
                options.TryReportProgress (LoginProgress.Authorizing);

                // If we couldn't retrieve accounts, use next fallback in chain.
                // If there is no fallback, complete current task with the same status (Faulted or Canceled).

                if (t.IsCanceled || t.IsFaulted) {
                    if (!loginWithNextProvider ())
                        tcs.SetFromTask (t);

                    return;
                }


                var accs = t.Result.ToArray ();

                // If there is an empty list of accounts, use next fallback in chain.
                // If there is no fallback, fail current task with an exception.

                if (accs.Length == 0) {
                    if (!loginWithNextProvider ())
                        tcs.SetException (new Exception ("No accounts found for this service."));

                    return;
                }


                // If there is just one account, sweet!
                // Complete current task with this account.

                if (accs.Length == 1) {
                    loginWithCurrentProvider (accs [0]);
                    return;
                }


                // If there is more than a single account, present an interface to choose one.
                // If fallback is available, add it to the list of options with null value.

                if (options.AccountChoiceProvider == null) {
                    tcs.SetException (new InvalidOperationException ("There is more than one account, but no accountChoiceProvider was specified."));
                    return;
                }

                var accOptions = accs.ToList ();

                if (currentProvider.Next != null)
                    accOptions.Add (null);

                Func<Account, string> titleForButton = (acc) => (acc != null) ? acc.Username : "Other";

                options.TryReportProgress (LoginProgress.PresentingAccountChoice);

                options.AccountChoiceProvider
                    .ChooseAsync (accOptions, titleForButton)
                    .ContinueWith (ct => {

                    options.TryReportProgress (LoginProgress.Authorizing);

                    // If the user didn't choose the account (e.g. by dismissing the popover),
                    // set current task as cancelled by user.

                    if (ct.IsCanceled || ct.IsFaulted) {
                        tcs.SetFromTask (ct);
                        return;
                    }

                    var acc = ct.Result;

                    // If the user has chosen an account from list, use it for our result

                    if (acc != null) {
                        loginWithCurrentProvider (acc);
                        return;
                    }

                    // If the user has chosen "Other" option, use next fallback in chain.
                    loginWithNextProvider ();

                });
            }, TaskScheduler.FromCurrentSynchronizationContext ());

            return tcs.Task;
        }

        LinkedList<AccountProvider> GetProviderChain (LoginOptions options, string [] scope)
        {
            var list = new LinkedList<AccountProvider> ();

            foreach (var serviceFactory in _fallbackChain) {
                var service = serviceFactory ();

                var scoped = service as ISupportScope;
                if (scoped != null && scope != null)
                    scoped.Scopes = scope;

                var system = service as IOSService;
                if (system != null)
                    system.AllowLoginUI = options.AllowLoginUI;

                list.AddLast (new AccountProvider (service, false));

                if (options.AllowLoginUI && service.SupportsAuthentication) {
                    if (options.PresentAuthController != null)
                        list.AddLast (new AccountProvider (service, options.PresentAuthController));
                    else
                        list.AddLast (new AccountProvider (service, true));
                }
            }

            return list;
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
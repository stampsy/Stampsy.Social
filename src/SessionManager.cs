using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Social.Services;
using Xamarin.Social;
using Xamarin.Auth;
using Stampsy.Social;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Newtonsoft.Json.Linq;
using Stampsy.Social.Providers;

namespace Stampsy.Social
{
    public abstract class SessionManager
    {
        class NaïveNetworkMonitor : INetworkMonitor {
            public bool IsNetworkAvailable {
                get { return true; }
            }
        }

        public static INetworkMonitor NetworkMonitor { get; set; }

        static SessionManager ()
        {
            NetworkMonitor = new NaïveNetworkMonitor ();
        }


        private ISessionProvider _provider;
        private Task<Session> _task;

        public EventHandler StateChanged;

        public SessionManager (params Func<Service> [] fallbackChain)
        {
            _provider = new FallbackSessionProvider (fallbackChain);
        }

        public bool IsLoggedIn {
            get { return State == ServiceState.LoggedIn; }
        }

        public bool IsAuthenticating {
            get { return State == ServiceState.Authenticating; }
        }

        public bool IsLoggedOut {
            get { return State == ServiceState.LoggedOut; }
        }

        public ServiceState State {
            get {
                var t = _task;

                if (t == null)
                    return ServiceState.LoggedOut;

                switch (t.Status) {
                    case TaskStatus.RanToCompletion:
                    return ServiceState.LoggedIn;
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                    return ServiceState.LoggedOut;
                    default:
                    return ServiceState.Authenticating;
                }
            }
        }

        public Session ActiveSession {
            get {
                Utils.EnsureMainThread ();

                if (!IsLoggedIn)
                    return null;

                return _task.Result;
            }
        }

        public Task<Session> GetSessionAsync (LoginOptions options, string [] scope = null)
        {
            return GetSessionAsync (() => _provider.Login (options, scope));
        }

        Task<Session> GetSessionAsync (Func<Task<Session>> sessionFactory)
        {
            Utils.EnsureMainThread ();

            if (IsLoggedOut) {
                _task = sessionFactory ();
                _task.ContinueWith (t => {
                    OnStateChanged ();
                }, TaskScheduler.FromCurrentSynchronizationContext ());

                OnStateChanged ();
            }

            return _task;
        }

        public void CloseSession ()
        {
            Utils.EnsureMainThread ();

            if (IsLoggedOut)
                return;

            if (IsAuthenticating)
                throw new InvalidOperationException ("Attempted to close session while authenticating. Use CloseSessionAsync to handle all cases.");

            TryDeleteActiveSessionAccount ();

            _task = null;
            OnStateChanged ();
        }

        void TryDeleteActiveSessionAccount ()
        {
            var active = ActiveSession;
            try {
                active.Service.DeleteAccount (active.Account);
            } catch {
                // Account doesn't exist, or operation isn't supported by service
            }
        }

        public Task CloseSessionAsync ()
        {
            return GetSessionAsync (LoginOptions.NoUI).ContinueWith (t => {
                CloseSession ();
            }, TaskScheduler.FromCurrentSynchronizationContext ());
        }

        protected virtual void OnStateChanged ()
        {
            if (StateChanged != null)
                StateChanged (this, EventArgs.Empty);
        }

        protected Session EnsureLoggedIn ()
        {
            if (IsLoggedOut)
                throw new InvalidOperationException ("You are not logged in.");

            return ActiveSession;
        }

        public Task SwitchSessionAsync (Session newSession, bool saveAccount)
        {
            return CloseSessionAsync ().ContinueWith (_ => {
                OpenSession (newSession, saveAccount);
            }, TaskScheduler.FromCurrentSynchronizationContext ());
        }

        void OpenSession (Session session, bool saveAccount)
        {
            Utils.EnsureMainThread ();

            if (session == null)
                throw new ArgumentNullException ("session");

            if (!IsLoggedOut)
                throw new InvalidOperationException ("Another session is already open.");

            var tcs = new TaskCompletionSource<Session> ();
            tcs.SetResult (session);

            if (saveAccount)
                session.Service.SaveAccount (session.Account);

            GetSessionAsync (() => tcs.Task);
        }
    }
}


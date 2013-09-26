using System;
using System.Threading;
using System.Threading.Tasks;
using Stampsy.Social.Providers;
using Service = Xamarin.Social.Service;
#if PLATFORM_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#elif PLATFORM_ANDROID
using Android.App;
#endif


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


        public SessionState State {
            get {
                var t = _task;

                if (t == null)
                    return SessionState.LoggedOut;

                switch (t.Status) {
                case TaskStatus.RanToCompletion:
                    return SessionState.LoggedIn;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    return SessionState.LoggedOut;
                default:
                    return SessionState.Authenticating;
                }
            }
        }

        private Task<Session> _task;
        private CancellationTokenSource _openSessionCts;
        private ISessionProvider _provider;

        public EventHandler StateChanged;

        public bool IsLoggedIn {
            get { return State == SessionState.LoggedIn; }
        }

        public bool IsAuthenticating {
            get { return State == SessionState.Authenticating; }
        }

        public bool IsLoggedOut {
            get { return State == SessionState.LoggedOut; }
        }

        public Session ActiveSession {
            get {
                Utils.EnsureMainThread ();

                if (!IsLoggedIn)
                    return null;

                return _task.Result;
            }
        }

        protected SessionManager (params Func<Service> [] fallbackChain)
        {
            _provider = new FallbackSessionProvider (fallbackChain);
        }

        public Task<Session> GetSessionAsync (LoginOptions options, string [] scope = null)
        {
            return GetSessionAsync (token =>
                _provider.Login (options, scope, token)
            );
        }

        public void CloseSession ()
        {
            Utils.EnsureMainThread ();

            if (IsLoggedOut)
                return;

            if (IsLoggedIn) {
                TryDeleteAccount (ActiveSession);
            } else if (IsAuthenticating) {
                _openSessionCts.Cancel (); /* Created by OpenSessionAsync */
                _openSessionCts.Dispose ();
                _openSessionCts = null;
            }

            _task = null;
            OnStateChanged ();
        }

        internal void SetSession (Session session, bool saveAccount)
        {
            Utils.EnsureMainThread ();

            if (session == null)
                throw new ArgumentNullException ("session");

            CloseSession ();


            if (saveAccount && session.Service.SupportsSave) {
#if PLATFORM_ANDROID
                session.Service.SaveAccount (session.Account, Application.Context);
#else
                session.Service.SaveAccount (session.Account);
#endif
            }

            GetSessionAsync (_ => Task.FromResult (session));
        }

        Task<Session> GetSessionAsync (Func<CancellationToken, Task<Session>> sessionFactory)
        {
            Utils.EnsureMainThread ();

            if (IsLoggedOut) {
                _openSessionCts = new CancellationTokenSource (); /* Invalidated by CloseSession */

                _task = OpenSessionAsync (sessionFactory, _openSessionCts.Token);
                OnStateChanged ();

                _task.ContinueWith (t => {
                    OnStateChanged ();
                }, TaskScheduler.FromCurrentSynchronizationContext ());
            }

            return _task;
        }

        async Task<Session> OpenSessionAsync (Func<CancellationToken, Task<Session>> sessionFactory, CancellationToken token)
        {
            Session session = await sessionFactory (token);

            if (token.IsCancellationRequested)
                TryDeleteAccount (session);

            token.ThrowIfCancellationRequested ();
            return session;
        }

        protected Session EnsureLoggedIn ()
        {
            if (IsLoggedOut)
                throw new InvalidOperationException ("You are not logged in.");

            return ActiveSession;
        }

        protected virtual void OnStateChanged ()
        {
            SynchronizationContext.Current.Post (_ => {
                if (StateChanged != null)
                    StateChanged (this, EventArgs.Empty);
            }, null);
        }


        static void TryDeleteAccount (Session session)
        {
            try {
#if PLATFORM_ANDROID
                session.Service.DeleteAccount (session.Account, Application.Context);
#else
                session.Service.DeleteAccount (session.Account);
#endif
            } catch {
                // Account doesn't exist, or operation isn't supported by service
            }
        }
    }
}


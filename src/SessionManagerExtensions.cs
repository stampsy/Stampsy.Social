using System;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Stampsy.Social
{
    internal static class SessionManagerExtensions
    {
        static Task<T> WithSession<T> (ServiceManager manager, Func<Task<T>> call, string [] scope, LoginOptions options, bool allowReauthorize, bool allowRetryLogin)
        {
            // Check connection

            if (!SessionManager.NetworkMonitor.IsNetworkAvailable)
                return Task.Factory.FromException<T> (new OfflineException ());

            // Make sure we are logged in

            return manager.GetSessionAsync (options, scope).ContinueWith (loginTask => {
                if (loginTask.IsCanceled || loginTask.IsFaulted)
                    return loginTask.Cast<T> ();

                var session = loginTask.Result;

                // Try to make the API call

                return call ().ContinueWith (callTask => {
                    if (!callTask.IsFaulted)
                        return callTask;

                    // The call failed--maybe we can reauthorize?

                    return ConsiderReauthorize (manager, session, callTask.Exception, allowReauthorize).ContinueWith (reauthTask => {
                        if (reauthTask.IsCanceled)
                            return reauthTask.Cast<T> ();

                        // Reauthorization succeeded--switch active session
                        // and retry the API call

                        if (!reauthTask.IsFaulted) {
                            var newSession = reauthTask.Result;

                            return manager.SwitchSessionAsync (newSession, true).ContinueWith (
                                _ => call (),
                                TaskScheduler.FromCurrentSynchronizationContext ()
                            ).Unwrap ();
                        }

                        // Reauthorization failed

                        bool mustLogout = reauthTask.Exception.InnerException as MustLogoutException != null;
                        if (!mustLogout)
                            return reauthTask.Cast<T> ();

                        manager.CloseSession ();

                        if (!allowRetryLogin)
                            return reauthTask.Cast<T> ();

                        return WithSession (manager, call, scope, options, allowReauthorize: false, allowRetryLogin: false);

                    }, TaskScheduler.FromCurrentSynchronizationContext ()).Unwrap ();
                }, TaskScheduler.FromCurrentSynchronizationContext ()).Unwrap ();
            }, TaskScheduler.FromCurrentSynchronizationContext ()).Unwrap ();
        }

        internal static Task WithSession (this ServiceManager manager, Func<Task> call, LoginOptions options, string [] scope = null)
        {
            return WithSession (manager, () => call ().MakeGeneric (), scope, options, true, true);
        }

        internal static Task<T> WithSession<T> (this ServiceManager manager, Func<Task<T>> call, LoginOptions options, string [] scope = null)
        {
            return WithSession (manager, call, scope, options, true, true);
        }

        static Task<Session> ConsiderReauthorize (ServiceManager manager, Session session, AggregateException aex, bool allowReauthorize)
        {
            var ex = aex.Flatten ().InnerException as ApiException;

            // We may have a chance at reauthorizing

            if (ex != null && ex.Kind == ApiExceptionKind.Unauthorized && allowReauthorize) {
                return session.Reauthorize ().ContinueWith (t => {
                    if (t.IsCanceled || t.IsFaulted)
                        throw new MustLogoutException (t.Exception);

                    return t.Result;
                });
            }

            var tcs = new TaskCompletionSource<Session> ();

            // If the call failed due to an unrelated error, there is no point to reauthorizing.
            // Otherwise, interpret it as a bad session that we just couldn't reauthorize.

            if (ex == null || ex.Kind == ApiExceptionKind.Other)
                tcs.SetException (aex);
            else
                tcs.SetException (new MustLogoutException (ex));

            return tcs.Task;
        }

        static Task<Session> Reauthorize (this Session session)
        {
            var service = session.Service;
            var account = session.Account;

            try {
                return service.ReauthorizeAsync (account).ContinueWith (t => {
                    return new Session (service, t.Result);
                });
            } catch (Exception ex) {
                return Task.Factory.FromException<Session> (ex);
            }
        }

        static Task<T> Cast<T> (this Task t)
        {
            var tcs = new TaskCompletionSource<T> ();
            t.ContinueWith (tcs.SetFromTask);
            return tcs.Task;
        }

        static Task<bool> MakeGeneric (this Task task)
        {
            return task.ContinueWith (t => {
                if (t.IsFaulted)
                    throw t.Exception;

                return true;
            }, TaskContinuationOptions.NotOnCanceled);

        }

        class MustLogoutException : Exception {
            public MustLogoutException (Exception inner)
                : base ("Could not reauthorize", inner)
            {
            }
        }
    }
}


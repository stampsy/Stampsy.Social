using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Stampsy.Social
{
    internal static class SessionManagerExtensions
    {
        static async Task<bool> Reauthorize (ServiceManager manager, Session session)
        {
            var service = session.Service;
            var account = session.Account;

            try {
                account = await service.ReauthorizeAsync (account);
                session = new Session (service, account);

                manager.CloseSession ();
                manager.SetSession (session, true);
                return true;
            } catch {
                return false;
            }
        }

        static async Task<T> WithSession<T> (ServiceManager manager, Func<Task<T>> call, string [] scope, LoginOptions options, CancellationToken token, bool allowReauthorizeOrLogout = true)
        {
            if (!SessionManager.NetworkMonitor.IsNetworkAvailable)
                throw new OfflineException ();

            var session = await manager.GetSessionAsync (options, scope);
            ApiException ex = null;

            try {
                return await call ();
            } catch (ApiException aex) {
                if (!allowReauthorizeOrLogout)
                    throw;

                ex = aex;
            } catch (Exception) {
                throw;
            }

            bool tryReauthorize = (ex.Kind == ApiExceptionKind.Unauthorized);
            if (tryReauthorize) {
                token.ThrowIfCancellationRequested ();
                if (await Reauthorize (manager, session)) {
                    token.ThrowIfCancellationRequested ();
                    return await call ();
                }
            }

            token.ThrowIfCancellationRequested ();

            bool tryLogout = (ex.Kind == ApiExceptionKind.Unauthorized || ex.Kind == ApiExceptionKind.Forbidden);
            if (tryLogout) {
                token.ThrowIfCancellationRequested ();
                manager.CloseSession ();
                return await WithSession (manager, call, scope, options, token, false);
            }

            throw ex;
        }

        internal static Task WithSession (this ServiceManager manager, Func<Task> call, LoginOptions options, CancellationToken token, string [] scope = null)
        {
            return WithSession (manager, async () => { await call (); return true; }, scope, options, token);
        }

        internal static Task<T> WithSession<T> (this ServiceManager manager, Func<Task<T>> call, LoginOptions options, CancellationToken token, string [] scope = null)
        {
            return WithSession (manager, call, scope, options, token);
        }
    }
}


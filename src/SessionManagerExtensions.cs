using System;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace Stampsy.Social
{
    internal static class SessionManagerExtensions
    {
        static async Task<T> WithSession<T> (ServiceManager manager, Func<Task<T>> call, string [] scope, LoginOptions options, bool allowReauthorizeOrLogout = true)
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
                Session reauthorizedSession = null;

                try {
                    var service = session.Service;
                    var badAccount = session.Account;
                    var reauthorizedAccount = await service.ReauthorizeAsync (badAccount);

                    reauthorizedSession = new Session (service, reauthorizedAccount);
                } catch { }

                if (reauthorizedSession != null) {
                    await manager.SwitchSessionAsync (reauthorizedSession, true);
                    return await call ();
                }
            }

            bool tryLogout = (ex.Kind == ApiExceptionKind.Unauthorized || ex.Kind == ApiExceptionKind.Forbidden);
            if (tryLogout) {
                await manager.CloseSessionAsync ();
                return await WithSession (manager, call, scope, options, false);
            }

            throw ex;
        }

        internal static Task WithSession (this ServiceManager manager, Func<Task> call, LoginOptions options, string [] scope = null)
        {
            return WithSession (manager, async () => { await call (); return true; }, scope, options);
        }

        internal static Task<T> WithSession<T> (this ServiceManager manager, Func<Task<T>> call, LoginOptions options, string [] scope = null)
        {
            return WithSession (manager, call, scope, options);
        }
    }
}


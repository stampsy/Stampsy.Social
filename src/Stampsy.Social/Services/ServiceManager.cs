using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xamarin.Auth;
using Xamarin.Social;
using Service = Xamarin.Social.Service;
#if PLATFORM_ANDROID
using Android.App;
#endif

namespace Stampsy.Social
{
    public abstract class ServiceManager : SessionManager
    {
        public abstract string Name { get; }
        public abstract string [] KnownServiceIds { get; }

        public ServiceManager (params Func<Service> [] fallbackChain)
            : base (fallbackChain)
        {
        }

#if PLATFORM_ANDROID
        public void DeleteStoredAccounts (Activity activity, string [] serviceIds = null)
#else
        public void DeleteStoredAccounts (string [] serviceIds = null)
#endif
        {
            serviceIds = serviceIds ?? KnownServiceIds;

#if PLATFORM_ANDROID
            var store = AccountStore.Create (activity);
#else
            var store = AccountStore.Create ();
#endif

            foreach (var serviceId in serviceIds) {
                foreach (var account in store.FindAccountsForService (serviceId).ToList ()) {
                    store.Delete (account, serviceId);
                }
            }
        }

        #region Public API

        public abstract Task<ServiceUser> GetProfileAsync (CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions));
        public abstract Task ShareAsync (Item item, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions));
        public abstract Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (Page<IEnumerable<ServiceUser>> previous = null, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions));

        public virtual Task<IDictionary<string, string>> GetTokenDataAsync (CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetTokenData (token),
                options,
                token
            );
        }

        #endregion

        #region Implementation

        protected virtual Task<IDictionary<string, string>> GetTokenData (CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            return session.Service.GetAccessTokenAsync (session.Account, token);
        }

        #endregion

        #region Parsing

        protected virtual ServiceUser ParseUser (JToken user)
        {
            throw new NotImplementedException ();
        }

        protected Gender? ParseGender (string gender, string femaleGender, string maleGender)
        {
            if (gender == femaleGender)
                return Gender.Female;

            if (gender == maleGender)
                return Gender.Male;

            return null;
        }

        protected Task<T> ParseAsync<T> (Request request, Func<JToken, T> parseJson, CancellationToken token)
        {
            return request.GetResponseAsync (token).ContinueWith ((responseTask) => {
                var json = GetResponseJson (responseTask);
                return parseJson (json);
            }, token, TaskContinuationOptions.None, TaskScheduler.Default);
        }

        protected JToken GetResponseJson (Task<Response> responseTask)
        {
            if (responseTask.IsFaulted)
                HandleResponseException (responseTask.Exception.Flatten ().InnerException);

            var response = responseTask.Result;
            var text = response.GetResponseText ();

            JToken json;
            try {
                json = JToken.Parse (text);
            } catch (Exception ex) {
                throw new ApiException ("Could not parse JSON.", response, ex, ApiExceptionKind.InvalidJson);
            }

            HandleResponseJson (response, json);
            return json;
        }

        protected virtual void HandleResponseException (Exception ex)
        {
        }

        protected virtual void HandleResponseJson (Response response, JToken json)
        {
        }

        #endregion

        #region Pagination

        protected virtual Task<Page<T>> ParsePageAsync<T> (Request request, Func<JToken, T> parseJson, CancellationToken token)
        {
            return request.GetResponseAsync (token).ContinueWith ((responseTask) => {
                var json = GetResponseJson (responseTask);
                var pageToken = ParsePageToken (json);

                return new Page<T> (parseJson (json), pageToken);
            }, token, TaskContinuationOptions.None, TaskScheduler.Default);
        }

        protected virtual string ParsePageToken (JToken json)
        {
            throw new NotImplementedException ();
        }

        #endregion
    }
}


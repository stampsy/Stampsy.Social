using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xamarin.Auth;
using Xamarin.Social;
using Xamarin.Social.Services;
using Stampsy.Social.Providers;

namespace Stampsy.Social.Services
{
    public class GoogleManager : ServiceManager
    {
        public override string Name {
            get { return "Google"; }
        }

        public override string [] KnownServiceIds {
            get {
                return new [] { "Google" };
            }
        }

        public GoogleManager (params Func<Service> [] fallbackChain)
            : base (fallbackChain)
        {
        }

        public static readonly Uri BaseOAuthUri = new Uri ("https://www.googleapis.com/oauth2/v2/");
        public static readonly Uri BaseApiUri = new Uri ("https://www.googleapis.com/plus/v1/");

        public const string UserinfoEmailScopeKey = "https://www.googleapis.com/auth/userinfo.email";
        public const string PlusMeScopeKey = "https://www.googleapis.com/auth/plus.me";
        public const string PlusLoginScopeKey = "https://www.googleapis.com/auth/plus.login";

        #region Public API

        public override Task<ServiceUser> GetProfileAsync (LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetProfile (),
                options,
                new [] {
                    UserinfoEmailScopeKey,
                    PlusMeScopeKey
                }
            );
        }

        public override Task ShareAsync (Item item, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            throw new NotImplementedException ();
        }

        public override Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (Page<IEnumerable<ServiceUser>> previous = null, LoginOptions options = default (LoginOptions))
        {
            return this.GetFriendsAsync (100, "best", previous, options);
        }

        public Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (int itemsPerPage = 100, string orderBy = "best", Page<IEnumerable<ServiceUser>> previous = null, LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetPeople (itemsPerPage, orderBy, previous),
                options,
                new [] { PlusLoginScopeKey }
            );
        }

        #endregion

        #region Implementation

        Task<Page<IEnumerable<ServiceUser>>> GetPeople (int itemsPerPage, string orderBy, Page<IEnumerable<ServiceUser>> previous)
        {
            var session = EnsureLoggedIn ();
            var uri = new Uri (BaseApiUri, "people/me/people/visible");

            var args = new Dictionary<string, string> {
                { "maxResults", itemsPerPage.ToString () },
                { "orderBy", orderBy }
            };

            if (previous != null) {
                args ["pageToken"] = previous.NextPageToken;
            }

            var request = session.Service.CreateRequest ("GET", uri, args, session.Account);
            return ParsePageAsync (request,
                (json) => json ["items"].Children<JObject> ().Select (ParseUser)
            );
        }

        Task<ServiceUser> GetProfile ()
        {
            return Task.Factory.ContinueWhenAll<ServiceUser> (new Task<ServiceUser> [] {
                GetGooglePlusProfile (),
                GetOAuthProfile ()
            }, (ts) => {
                var plusProfile = ((Task<ServiceUser>) ts [0]).Result;
                var oauthProfile = ((Task<ServiceUser>) ts [1]).Result;

                return new ServiceUser () {
                    Email = oauthProfile.Email,
                    FirstName = plusProfile.FirstName,
                    Id = plusProfile.Id,
                    ImageUrl = plusProfile.ImageUrl,
                    LastName = plusProfile.LastName,
                    Location = plusProfile.Location,
                    Name = plusProfile.Name,
                    Nickname = plusProfile.Nickname
                };
            });
        }

        Task<ServiceUser> GetGooglePlusProfile ()
        {
            var session = EnsureLoggedIn ();
            var request = session.Service.CreateRequest (
                "GET",
                new Uri (BaseApiUri, "people/me"),
                session.Account
            );

            return ParseAsync (request, ParseProfile);
        }

        Task<ServiceUser> GetOAuthProfile ()
        {
            var session = EnsureLoggedIn ();
            var request = session.Service.CreateRequest (
                "GET",
                new Uri (BaseOAuthUri, "userinfo"),
                session.Account
            );

            return ParseAsync (request, ParseProfile);
        }

        #endregion

        protected override void HandleResponseException (Exception ex)
        {
            var wex = ex as WebException;
            if (wex == null || wex.Status != WebExceptionStatus.ProtocolError)
                return;

            switch (((HttpWebResponse) wex.Response).StatusCode) {
                case HttpStatusCode.Unauthorized:
                throw new ApiException ("Unauthorized", wex, ApiExceptionKind.Unauthorized);
                case HttpStatusCode.Forbidden:
                throw new ApiException ("Forbidden", wex, ApiExceptionKind.Forbidden);
            }
        }

        protected override void HandleResponseJson (Response response, JToken json)
        {
            if (json.Type != JTokenType.Object)
                return;

            var err = json ["error"];
            if (err == null)
                return;

            var msg = err.Value<string> ("message");
            throw new ApiException (msg, response, ApiExceptionKind.Other);
        }

        protected override string ParsePageToken (JToken json)
        {
            string next = json.Value<string> ("nextPageToken");
            return !string.IsNullOrWhiteSpace (next)
                ? next
                : null;
        }

        private ServiceUser ParseProfile (JToken user)
        {
            var profile = ParseUser (user);

            if (!String.IsNullOrWhiteSpace (profile.ImageUrl)) {
                // getting full-size picture
                var queryIdx = profile.ImageUrl.IndexOf ("?");
                profile.ImageUrl = profile.ImageUrl.Substring (0, queryIdx);
            }

            return profile;
        }

        protected override ServiceUser ParseUser (JToken user)
        {
            return new ServiceUser {
                Id = user.Value<string> ("id"),
                Name = user.Value<string> ("displayName"),
                Email = user.Value<string> ("email"),
                FirstName = user.Value<string> ("name", "givenName"),
                LastName = user.Value<string> ("name", "familyName"),
                ImageUrl = user.Value<string> ("image", "url"),
                Gender = ParseGender (user.Value<string> ("gender"), "female", "male")
            };
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;
using Xamarin.Auth;
using Xamarin.Social.Services;
using Stampsy.Social.Providers;
using Xamarin.Social;

namespace Stampsy.Social.Services
{
    public class TwitterManager : ServiceManager
    {
        private static readonly Uri BaseUri = new Uri ("https://api.twitter.com/1.1/");

        public override string Name {
            get { return "Twitter"; }
        }

        public override string [] KnownServiceIds {
            get {
                return new [] { "Twitter" };
            }
        }

        public TwitterManager (params Func<Service> [] fallbackChain)
            : base (fallbackChain)
        {
        }

        #region Public API

        public override Task<ServiceUser> GetProfileAsync (CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetProfile (token),
                options,
                token
            );
        }

        public override Task ShareAsync (Item item, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.Share (item, token),
                options,
                token
            );
        }

        public override Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (Page<IEnumerable<ServiceUser>> previous = null, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return GetFriendsAsync (100, previous, token, options);
        }

        public Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (int itemsPerPage = 100, Page<IEnumerable<ServiceUser>> previous = null, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetFriends (itemsPerPage, previous, token),
                options,
                token
            );
        }

        #endregion

        #region Implementation

        Task<Page<IEnumerable<ServiceUser>>> GetFriends (int itemsPerPage, Page<IEnumerable<ServiceUser>> previous, CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            var request = session.Service.CreateRequest (
                "GET",
                new Uri (BaseUri, "friends/list.json"),
                new Dictionary<string, string> { 
                    { "count", itemsPerPage.ToString () },
                    { "cursor", (previous != null) ? previous.NextPageToken : "-1" },
                    { "skip_status", "true" },
                    { "include_user_entities", "false" }
                },
                session.Account
            );

            return ParsePageAsync (request,
                (json) => json ["users"].Children<JObject> ().Select (ParseUser),
                token
            );
        }

        Task<ServiceUser> GetProfile (CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            var request = session.Service.CreateRequest (
                "GET",
                new Uri ("https://api.twitter.com/1.1/users/show.json"),
                new Dictionary<string, string> { { "screen_name", session.Account.Username } },
                session.Account
            );

            return ParseAsync (request, ParseProfile, token);
        }

        Task Share (Item item, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            //
            // Combine the links into the tweet
            //
            var sb = new StringBuilder ();
            sb.Append (item.Text);
            foreach (var l in item.Links) {
                sb.Append (" ");
                sb.Append (l.AbsoluteUri);
            }
            var status = sb.ToString ();

            //
            // Create the request
            //
            Request req;
            if (item.Images.Count == 0) {
                req = session.Service.CreateRequest ("POST", new Uri ("https://api.twitter.com/1.1/statuses/update.json"), session.Account);
                req.Parameters["status"] = status;
            }
            else {
                req = session.Service.CreateRequest ("POST", new Uri ("https://api.twitter.com/1.1/statuses/update_with_media.json"), session.Account);
                req.AddMultipartData ("status", status);
                foreach (var i in item.Images.Take (session.Service.MaxImages)) {
                    i.AddToRequest (req, "media[]");
                }
            }

            //
            // Send it
            //
            return req.GetResponseAsync (token);
        }

        #endregion

        protected override void HandleResponseException (Exception ex)
        {
            var sex = ex as SocialException;
            if (sex != null)
                throw new ApiException ("Seems like iOS account got deleted.", sex, ApiExceptionKind.Unauthorized);
        }

        protected override void HandleResponseJson (Response response, JToken json)
        {
            if (json.Type != JTokenType.Object)
                return;

            var errs = json ["errors"];
            if (errs == null)
                return;

            var err = errs [0];

            var code = err.Value<int> ("code");
            var msg = err.Value<string> ("message");

            switch (code) {
                case 215: // Bad Authentication data
                throw new ApiException (msg, code, response, ApiExceptionKind.Unauthorized);
                default:
                throw new ApiException (msg, code, response, ApiExceptionKind.Other);
            }
        }

        protected override string ParsePageToken (JToken json)
        {
            var nextCursor = json.Value<long> ("next_cursor");

            return (nextCursor != 0)
                ? nextCursor.ToString ()
                : null;
        }

        private ServiceUser ParseProfile (JToken user)
        {
            var profile = ParseUser (user);

            // getting full-size picture
            profile.ImageUrl = profile.ImageUrl.Replace ("_normal.", ".");

            return profile;
        }

        protected override ServiceUser ParseUser (JToken user)
        {
            return new ServiceUser {
                Id = user.Value<string> ("id_str"),
                Name = user.Value<string> ("name"),
                ImageUrl = user.Value<string> ("profile_image_url"),
                Nickname = user.Value<string> ("screen_name"),
                Location = user.Value<string> ("location"),
                Gender = null
            };
        }
    }
}


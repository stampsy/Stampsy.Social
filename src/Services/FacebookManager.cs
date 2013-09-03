using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xamarin.Auth;
using Xamarin.Social.Services;
using Xamarin.Social;
using Stampsy.Social.Providers;
using System.IO;
using Newtonsoft.Json;

namespace Stampsy.Social.Services
{
    public class FacebookManager : ServiceManager
    {
        public static readonly Uri BaseUri = new Uri ("https://graph.facebook.com/");

        public override string Name {
            get { return "Facebook"; }
        }

        public override string [] KnownServiceIds {
            get {
                return new [] { "Facebook" };
            }
        }

        public FacebookManager (params Func<Service> [] fallbackChain)
            : base (fallbackChain)
        {
        }

        #region Public API

        public override Task<ServiceUser> GetProfileAsync (CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetProfile (token),
                options,
                token,
                new [] { "email" }
            );
        }

        public override Task ShareAsync (Item item, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.Share (item, token),
                options,
                token,
                new [] { "publish_actions" }
            );
        }

        public override Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (Page<IEnumerable<ServiceUser>> previous = null, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return GetFriendsAsync (100, previous, token, options);
        }

        public Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (int count = 100, Page<IEnumerable<ServiceUser>> previous = null, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetFriends (count, previous, token),
                options,
                token,
                new [] { "email" }
            );
        }

        public Task<FacebookLikeInfo> LikeAsync (string url, string objectId, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.Like (url, objectId, token),
                options,
                token,
                new [] { "publish_actions" }
            );
        }

        public Task UnlikeAsync (string url, string likeId, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.Unlike (url, likeId, token),
                options,
                token,
                new [] { "publish_actions" }
            );
        }

        public Task<FacebookLikeInfo> GetLikeInfoAsync (string url, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetLikeInfo (url, token),
                options,
                token,
                new [] { "publish_actions" }
            );
        }

        #endregion

        #region Implementation

        Task<Page<IEnumerable<ServiceUser>>> GetFriends (int count, Page<IEnumerable<ServiceUser>> previous, CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            var pageUrl = (previous != null) ? previous.NextPageToken : null;

            var request = session.Service.CreateRequest (
                "GET",
                (pageUrl != null) ? new Uri (pageUrl) : new Uri (BaseUri, "me/friends"),
                (pageUrl != null) ? null : new Dictionary<string, string> { { "limit", count.ToString () } },
                session.Account
            );

            return ParsePageAsync (
                request,
                (json) => json ["data"].Children<JObject> ().Select (ParseUser),
                token
            );
        }

        Task<ServiceUser> GetProfile (CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            var request = session.Service.CreateRequest (
                "GET",
                new Uri (BaseUri, "me"),
                session.Account
            );

            return ParseAsync (request, ParseUser, token);
        }

        Task Share (Item item, CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            Request req;

            if (item.Images.Count > 0) {
                req = session.Service.CreateRequest ("POST", new Uri (BaseUri, "me/photos"), session.Account);
                item.Images.First ().AddToRequest (req, "source");

                var message = new StringBuilder ();
                message.Append (item.Text);
                foreach (var l in item.Links) {
                    message.AppendLine ();
                    message.Append (l.AbsoluteUri);
                }
                req.AddMultipartData ("message", message.ToString ());
            }
            else {
                req = session.Service.CreateRequest ("POST", new Uri (BaseUri, "me/feed"), session.Account);
                req.Parameters["message"] = item.Text;
                if (item.Links.Count > 0) {
                    req.Parameters["link"] = item.Links.First ().AbsoluteUri;
                }
            }

            return ParseAsync (req, ParseShareResult, token);
        }

        bool ParseShareResult (JToken json)
        {
            if (json.Value<string> ("id") == null)
                throw new SocialException ("Facebook returned an unrecognized response:\n" + json.ToString ());

            return true;
        }

        Task<FacebookLikeInfo> Like (string url, string objectId, CancellationToken token)
        {
            if (objectId != null)
                return LikeGraphObject (url, objectId, token);

            return GetGraphObjectId (url, token).ContinueWith (t => {
                return LikeGraphObject (url, t.Result, token);
            }).Unwrap ();
        }

        Task<FacebookLikeInfo> LikeGraphObject (string url, string objectId, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            var req = session.Service.CreateRequest ("POST", new Uri (BaseUri, "me/og.likes"), session.Account);
            req.Parameters ["object"] = objectId;

            return req.GetResponseAsync (token).ContinueWith<JToken> (GetResponseJson).ContinueWith (t => {
                string likeId = null;
                if (t.IsFaulted) {
                    var ex = t.Exception.Flatten ().InnerException as ApiException; 
                    if (ex != null && ex.Code == 3501) { // already liked
                        int pos = ex.Message.IndexOf ("Action ID:");
                        if (pos != -1) {
                            likeId = ex.Message.Substring (pos + 10).Trim ();
                        }
                    }
                } else {
                    likeId = t.Result.Value<string> ("id");
                }

                if (likeId == null) {
                    throw new ApiException ("Could not get Like result.", t.Exception, ApiExceptionKind.Other);
                }

                return new FacebookLikeInfo {
                    ObjectId = objectId,
                    LikeId = likeId
                };
            });
        }

        Task<string> GetGraphObjectId (string url, CancellationToken token, bool scrape = false)
        {
            var session = EnsureLoggedIn ();

            var req = session.Service.CreateRequest ("POST", BaseUri, session.Account);
            req.Parameters ["id"] = url;
            req.Parameters ["scrape"] = scrape.ToString ();

            return req.GetResponseAsync (token).ContinueWith<JToken> (GetResponseJson).ContinueWith (t => {
                // TODO: check it's 'no such object' error
                if (t.Result.Type == JTokenType.Boolean && !(bool)t.Result) {
                    return GetGraphObjectId (url, token, true);
                }

                return Task.FromResult (t.Result.Value<string> ("id"));
            }).Unwrap ();
        }

        Task Unlike (string url, string likeId, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            var req = session.Service.CreateRequest ("DELETE", new Uri (BaseUri, likeId), session.Account);
            return req.GetResponseAsync (token).ContinueWith<JToken> (GetResponseJson);
        }

        Task<FacebookLikeInfo> GetLikeInfo (string url, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            var likeRequest = session.Service.CreateRequest ("GET", new Uri (BaseUri, "me/og.likes"), session.Account);
            likeRequest.Parameters ["object"] = url;

            var likeCountRequest = session.Service.CreateRequest ("GET", new Uri (BaseUri, "fql"), session.Account);
            likeCountRequest.Parameters["q"] = string.Format ("SELECT total_count, comments_fbid FROM link_stat WHERE url=\"{0}\"", url);

            var req = CreateBatchRequest (likeRequest, likeCountRequest);

            return req.GetResponseAsync (token).ContinueWith<JToken> (GetResponseJson).ContinueWith (t => {
                var likeInfo = new FacebookLikeInfo ();
                var data = (JArray) t.Result;

                var likeRes = JObject.Parse (data[0].Value<string> ("body"));
                if (likeRes["error"] != null && likeRes["error"].Value<int> ("code") == 1) {
                    // there is no graph object for the stamp
                    return likeInfo;
                }

                var likeCountRes = JObject.Parse (data[1].Value<string> ("body"));
                var likeCountData = likeCountRes["data"][0];

                likeInfo.LikeCount = likeCountData.Value<uint> ("total_count");
                likeInfo.ObjectId = likeCountData.Value<string> ("comments_fbid");

                // likeData = null: graph object exists, but user didn't like it
                if (((JArray) likeRes ["data"]).Count > 0) {
                    var likeData = likeRes["data"][0];
                    likeInfo.LikeId = likeData.Value<string> ("id");
                }

                return likeInfo;
            });
        }

        public struct FacebookLikeInfo
        {
            public string ObjectId { get; set; }
            public string LikeId { get; set; }
            public uint LikeCount { get; set; }
        }

        #endregion

        public Request CreateBatchRequest (params Request [] requests)
        {
            var session = EnsureLoggedIn ();
            JArray jsonRequests = new JArray ();

            foreach (var request in requests) {
                var jsonRequest = new JObject {
                    {"method", request.Method},
                    {"relative_url", GetRelativeUri (request).ToString ()}
                };

                var body = request.GetRawBody ();
                if (body.Length > 0 && request.Method == "POST") 
                    jsonRequest ["body"] = body;

                jsonRequests.Add (jsonRequest);
            }

            var batchRequest = session.Service.CreateRequest ("POST", BaseUri, session.Account);

            batchRequest.Parameters["batch"] = JsonConvert.SerializeObject (jsonRequests, Formatting.None);

            return batchRequest;
        }

        static Uri GetRelativeUri (Request request)
        {
            var url = request.Url.AbsoluteUri;

            if (request.Parameters.Count > 0 && request.Method != "POST") {
                var head = url.Contains ('?') ? "&" : "?";
                foreach (var p in request.Parameters) {
                    url += head;
                    url += Uri.EscapeDataString (p.Key);
                    url += "=";
                    url += Uri.EscapeDataString (p.Value);
                    head = "&";
                }
            }

            return BaseUri.MakeRelativeUri (new Uri (url));
        }

        protected override string ParsePageToken (JToken json)
        {
            string next = null;

            var paging = json ["paging"];
            if (paging != null) {
                next = paging.Value<string> ("next");
            }

            return !string.IsNullOrWhiteSpace (next)
                ? next
                : null;
        }

        protected override void HandleResponseException (Exception ex)
        {
            var wex = ex as WebException;
            if (wex == null || wex.Status != WebExceptionStatus.ProtocolError)
                return;

            var response = (HttpWebResponse) wex.Response;

            using (var stream = response.GetResponseStream ())
            using (var sr = new StreamReader (stream))
            using (var jr = new JsonTextReader (sr)) {
                JObject obj = null;
                try {
                    obj = (JObject) JObject.ReadFrom (jr);
                } catch {}

                if (obj != null) {
                    string msg = obj["error"].Value<string> ("message");
                    int code = obj["error"].Value<int> ("code");
                    var kind = GetExceptionKind (code);

                    throw new ApiException (msg, code, wex, kind);
                }
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new ApiException ("Unauthorized", wex, ApiExceptionKind.Unauthorized);
        }

        protected override void HandleResponseJson (Response response, JToken json)
        {
            if (json.Type != JTokenType.Object) 
                return;

            var err = json ["error"];
            if (err == null)
                return;

            var msg = err.Value<string> ("message");
            var code = err.Value<int> ("code");
            var kind = GetExceptionKind (code);

            throw new ApiException (msg, code, response, kind);
        }

        ApiExceptionKind GetExceptionKind (int facebookErrorCode)
        {
            // https://developers.facebook.com/docs/reference/api/errors/

            switch (facebookErrorCode) {
            // OAuth
            case 190: // The access token was invalidated on the device.
            case 102:
            case 2500: // An active access token must be used to query information about the current user.
            // Permissions
            case 10:
                return ApiExceptionKind.Unauthorized;
            default:
                // Permissions, again
                if (facebookErrorCode >= 200 && facebookErrorCode <= 299) // The user hasn't authorized the application to perform this action
                    return ApiExceptionKind.Unauthorized;

                return ApiExceptionKind.Other;
            }
        }

        protected override ServiceUser ParseUser (JToken user)
        {
            return new ServiceUser {
                Id = user.Value<string> ("id"),
                Name = user.Value<string> ("name"),
                FirstName = user.Value<string> ("first_name"),
                LastName = user.Value<string> ("last_name"),
                Nickname = user.Value<string> ("username"),
                Email = user.Value<string> ("email"),
                Location = user.Value<string> ("location", "name"),
                ImageUrl = string.Format ("http://graph.facebook.com/{0}/picture", user.Value<string> ("id")),
                Gender = ParseGender (user.Value<string> ("gender"), "female", "male")
            };
        }
    }
}


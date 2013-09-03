using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using Xamarin.Auth;
using Xamarin.Social;
using Xamarin.Social.Services;
using Newtonsoft.Json.Linq;

namespace Stampsy.Social
{
    public class DropboxManager : ServiceManager
    {
        public DropboxManager (params Func<Service> [] fallbackChain)
            : base (fallbackChain)
        {
        }

        public override string Name {
            get { return "Dropbox"; }
        }

        public override string [] KnownServiceIds {
            get {
                return new [] { "Dropbox" };
            }
        }

        #region Public API

        public static readonly Uri BaseContentUri = new Uri ("https://api-content.dropbox.com/1/");
        public static readonly Uri BaseApiUri = new Uri ("https://api.dropbox.com/1/");


        public Task<Metadata> GetMetadataAsync (string path, bool includeContent, string hash, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetMetadata (path, includeContent, hash, token),
                options,
                token
            );
        }

        public Task<Metadata> LoadThumbnailAsync (string path, string size, string destPath, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.LoadThumbnail (path, size, destPath, token),
                options,
                token
            );
        }

        public Task<Metadata> LoadFileAsync (string path, string destPath, Action<float> progress, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.LoadFile (path, destPath, progress, token),
                options,
                token
            );
        }

        public override Task<ServiceUser> GetProfileAsync (CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            return this.WithSession (
                () => this.GetProfile (token),
                options,
                token
            );
        }

        public override Task ShareAsync (Item item, CancellationToken token = default(CancellationToken), LoginOptions options = default (LoginOptions))
        {
            throw new NotImplementedException ();
        }

        public override Task<Page<IEnumerable<ServiceUser>>> GetFriendsAsync (Page<IEnumerable<ServiceUser>> previous = null, CancellationToken token = default (CancellationToken), LoginOptions options = default (LoginOptions))
        {
            throw new NotImplementedException ();
        }

        #endregion

        #region Implementation

        Task<Metadata> LoadThumbnail (string path, string size, string destPath, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            var uri = FormatUri ((DropboxService) session.Service, BaseContentUri, "thumbnails/{root}{path}", path);
            var request = session.Service.CreateRequest ("GET", uri, session.Account);
            request.Parameters ["size"] = size;

            return request.GetResponseAsync (token).ContinueWith (t => {
                Response res = t.Result;
                var metadataText = res.Headers["x-dropbox-metadata"];
                var metadata = ParseMetadata (JToken.Parse (metadataText));

                var s = res.GetResponseStream ();
                using (var fs = File.Create (destPath)) {
                    s.CopyTo (fs);
                }

                return metadata;
            }, token);
        }

        Task<Metadata> LoadFile (string path, string destPath, Action<float> progress, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            var uri = FormatUri ((DropboxService) session.Service, BaseContentUri, "files/{root}{path}", path);
            var request = session.Service.CreateRequest ("GET", uri, session.Account);

            return request.GetResponseAsync (token).ContinueWith (responseTask => {
                Response res = responseTask.Result;
                var metadataText = res.Headers ["x-dropbox-metadata"];
                var metadata = ParseMetadata (JToken.Parse (metadataText));

                const int BufferLength = 16 * 1024;

                var state = new LoadFileAsyncState {
                    OutputStream = File.Create (destPath),
                    ResponseStream = res.GetResponseStream (),
                    OnProgress = progress,
                    Metadata = metadata,
                    TotalBytes = Int64.Parse (res.Headers["Content-Length"]),
                    Buffer = new byte [BufferLength],
                    BufferLength = BufferLength
                };

                return ReadNextChunksAsync (state, token).ContinueWith (downloadTask => {
                    state.Dispose ();

                    if (downloadTask.IsCanceled || downloadTask.IsFaulted) {
                        try {
                            File.Delete (destPath);
                        } catch { }
                    }

                    return downloadTask.Result;
                });
            }).Unwrap ();
        }

        struct LoadFileAsyncState : IDisposable
        {
            public byte [] Buffer;
            public int BufferLength;
            public long BytesLoaded { get; set; }
            public long TotalBytes { get; set; }

            public FileStream OutputStream { get; set; }
            public Stream ResponseStream { get; set; }
            public Action<float> OnProgress { get; set; }
            public Metadata Metadata { get; set; }

            public void Dispose ()
            {
                ResponseStream.Close ();
                OutputStream.Close ();
            }
        }

        Task<Metadata> ReadNextChunksAsync (LoadFileAsyncState state, CancellationToken token)
        {
            return Task.Factory.FromAsync<byte[], int, int, int> (
                state.ResponseStream.BeginRead,
                state.ResponseStream.EndRead,
                state.Buffer, 0, state.BufferLength, null
            ).ContinueWith (t => {
                int bytesRead = t.Result;
                if (bytesRead == 0)
                    return Task.FromResult (state.Metadata);

                state.BytesLoaded += bytesRead;

                if (state.OnProgress != null)
                    state.OnProgress (((float) state.BytesLoaded) / state.TotalBytes);

                state.OutputStream.Write (state.Buffer, 0, bytesRead);

                token.ThrowIfCancellationRequested ();
                return ReadNextChunksAsync (state, token);

            }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default).Unwrap ();
        }

        Task<Metadata> GetMetadata (string path, bool includeContent, string hash, CancellationToken token)
        {
            var session = EnsureLoggedIn ();

            var uri = FormatUri ((DropboxService) session.Service, BaseApiUri, "metadata/{root}{path}", path);
            var request = session.Service.CreateRequest ("GET", uri, session.Account);

            if (!string.IsNullOrWhiteSpace (hash))
                request.Parameters ["hash"] = hash;

            return ParseAsync (request, ParseMetadata, token);
        }

        Uri FormatUri (DropboxService service, Uri baseUri, string format, string path)
        {
            var relUrl = format.Replace ("{root}", service.Root).Replace ("{path}", path);
            #pragma warning disable 612, 618
            return new Uri (baseUri, Uri.EscapeDataString (relUrl), true);
            #pragma warning restore 612, 618
        }

        Task<ServiceUser> GetProfile (CancellationToken token)
        {
            var session = EnsureLoggedIn ();
            var request = session.Service.CreateRequest (
                "GET", new Uri (BaseApiUri, "account/info"), session.Account
            );

            return ParseAsync (request, ParseUser, token);
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

        protected override ServiceUser ParseUser (JToken user)
        {
            return new ServiceUser {
                Id = user.Value<string> ("uid"),
                Name = user.Value<string> ("display_name"),
            };
        }

        Metadata ParseMetadata (JToken json)
        {
            var data = new Metadata {
                Size = json.Value<string> ("size"),
                Hash = json.Value<string> ("hash"),
                TotalBytes = json.Value<long> ("bytes"),
                ThumbnailExists = json.Value<bool> ("thumb_exists"),
                Revision = json.Value<string> ("rev"),
                Modified = json["modified"] == null ? (DateTime?) null : DateTime.Parse (json.Value<string> ("modified")),
                ClientModified = json["client_mtime"] == null ? (DateTime?) null : DateTime.Parse (json.Value<string> ("client_mtime")),
                Path = json.Value<string> ("path"),
                IsDirectory = json.Value<bool> ("is_dir"),
                Icon = json.Value<string> ("icon"),
                Root = json.Value<string> ("root")
            };

            var contentsJson = json.Value<JArray> ("contents");
            if (contentsJson != null)
                data.Contents = contentsJson.Select (ParseMetadata).ToArray ();

            return data;
        }

        public struct Metadata
        {
            private Metadata [] _contents;

            public Metadata [] Contents {
                get { return _contents ?? new Metadata [0]; }
                set { _contents = value; }
            }

            public string Size { get; set; }
            public string Hash { get; set; }
            public long TotalBytes { get; set; }
            public bool ThumbnailExists { get; set; }
            public string Revision { get; set; }
            public DateTime? Modified { get; set; }
            public DateTime? ClientModified { get; set; }
            public string Path { get; set; }
            public bool IsDirectory { get; set; }
            public string Icon { get; set; }
            public string Root { get; set; }

            public string Name {
                get { return System.IO.Path.GetFileNameWithoutExtension (Path); }
            }

            public string Extension {
                get { return System.IO.Path.GetExtension (Path); }
            }
        }
    }
}
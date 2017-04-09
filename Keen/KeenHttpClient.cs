using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// TODO : Fill in comments in this file.
    /// </summary>
    internal class KeenHttpClient : IKeenHttpClient
    {
        private static readonly string JSON_CONTENT_TYPE = "application/json";
        private static readonly string AUTH_HEADER_KEY = "Authorization";

        private static readonly IEnumerable<KeyValuePair<string, string>> DEFAULT_HEADERS =
            new [] { new KeyValuePair<string, string>("Keen-Sdk", KeenUtil.GetSdkVersion()) };


        // NOTE : We don't dispose this, should we? The point of the cache is to keep them alive
        // and share these across usages. Our internal cache impl uses WeakRefs so as to allow
        // these to get garbage collected if all refs go away. If we were to make a 1:1 mapping
        // of KeenHttpClient to its HttpClient, then really we'd want to cache and share
        // instances of KeenHttpClient, right?
        private readonly HttpClient _httpClient = null;

        private class LoggingHttpHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                // TODO : Log stuff before and after request.

                // Now dispatch to the inner handler via the base impl.
                return base.SendAsync(request, cancellationToken);
            }
        }


        // NOTE : BaseUrl should have a final slash or the last Uri part is discarded. Also,
        // relative urls can *not* start with a slash.
        private KeenHttpClient(Uri baseUrl,
                               IHttpClientProvider httpClientProvider,
                               HttpMessageHandler handlerChain)
        {
            if (!baseUrl.IsAbsoluteUri)
            {
                throw new ArgumentException(
                    "The given base Url must be in the form of an absolute Uri.",
                    nameof(baseUrl));
            }

            _httpClient = httpClientProvider.GetOrCreateForUrl(baseUrl,
                                                               handlerChain,
                                                               KeenHttpClient.DEFAULT_HEADERS);
        }


        private static HttpMessageHandler CreateHandlerChainInternal(
            HttpClientHandler innerHandler,
            IEnumerable<DelegatingHandler> handlers)
        {
            // NOTE : There is no WebProxy available to the PCL profile, so we have to create an
            // IWebProxy implementation manually. Proxy is only supported on HttpClientHandler, and
            // not directly on DelegatingHandler, so handle that too. Basically only support Proxy
            // if client code does *not* give us an HttpClientHandler. Or else set the Proxy on 
            // their handler, but make sure it's not already set.

            // Example of how setting Proxy works in big .NET where the WebProxy class exists:
            //
            // new HttpClientHandler()
            // {
            //     Proxy = WebProxy("http://localhost:8888", false),
            //     UseProxy = true
            // };


            // TODO : Also, to support Proxy, we have to realize we'd be turning it on for a given
            // HttpClientHandler already installed for the HttpClient in the cache for a given URL.
            // Since modifications aren't allowed for HttpClients/*Handlers, we would replace the 
            // HttpClient, which would affect all future users of the cache requesting an
            // HttpClient for that URL, when really we want an abstraction to keep that sort of
            // setting tied to, which maybe is this KeenHttpClient.


            HttpMessageHandler handlerChain = (innerHandler ?? new HttpClientHandler());

            if (null == handlers)
            {
                return handlerChain;
            }

            foreach (var handler in handlers.Reverse())
            {
                if (null == handler)
                {
                    throw new ArgumentNullException(nameof(handlers),
                        "One of the given DelegatingHandler params was null.");
                }

                // This will throw if the given handler has already started any requests.
                // Basically all properties on all HttpClient/*Handler variations call
                // CheckDisposedOrStarted() in any setter, so the entire HttpClient is pretty much
                // locked down once it starts getting used.
                handler.InnerHandler = handlerChain;
                handlerChain = handler;
            }

            return handlerChain;
        }

        private static IEnumerable<DelegatingHandler> CreateDefaultDelegatingHandlers()
        {
            // TODO : Put more custom handlers in here, like retry/failure/proxy/logging handlers.

            // Create these every time, since *Handlers can't have properties changed after they've
            // started handling requests for an HttpClient.
            return new[] { new LoggingHttpHandler() };
        }

        internal static HttpMessageHandler CreateDefaultHandlerChain()
        {
            return KeenHttpClient.CreateHandlerChainInternal(
                null,
                KeenHttpClient.CreateDefaultDelegatingHandlers());
        }

        internal static HttpMessageHandler CreateHandlerChain(params DelegatingHandler[] handlers)
        {
            return KeenHttpClient.CreateHandlerChain(null, handlers);
        }

        internal static HttpMessageHandler CreateHandlerChain(HttpClientHandler innerHandler,
                                                              params DelegatingHandler[] handlers)
        {
            // We want our handlers last so our required stuff isn't overridden.
            IEnumerable<DelegatingHandler> intermediateHandlers =
                handlers.Concat(KeenHttpClient.CreateDefaultDelegatingHandlers());

            return KeenHttpClient.CreateHandlerChainInternal(innerHandler, intermediateHandlers);
        }

        // Not exposed so that 3rd party code doesn't have an easy way to build a KeenHttpClient
        // without our handlers installed.
        private static KeenHttpClient Create(Uri baseUrl,
                                             IHttpClientProvider httpClientProvider,
                                             HttpMessageHandler handlerChain)
        {
            var newClient = new KeenHttpClient(baseUrl, httpClientProvider, handlerChain);

            return newClient;
        }

        internal static KeenHttpClient Create(Uri baseUrl, IHttpClientProvider httpClientProvider)
        {
            return KeenHttpClient.Create(baseUrl,
                                         httpClientProvider,
                                         KeenHttpClient.CreateDefaultHandlerChain());
        }


        /// <summary>
        /// Construct a KeenClient with the provided project settings and HTTP handlers. We will
        /// construct the HTTP handler pipeline such that provided handlers are called in order for
        /// requests, and receive responses in reverse order. The last DelegatingHandler will defer
        /// to Keen internal handlers, and the pipeline will terminate at our own handler, which
        /// will do nothing and defer to the given HttpClientHandler if present, in case client
        /// code wants to do something like use WebRequestHandler functionality or otherwise add
        /// custom behavior.
        /// </summary>


        internal static KeenHttpClient Create(Uri baseUrl,
                                              IHttpClientProvider httpClientProvider,
                                              HttpClientHandler innerHandler,
                                              params DelegatingHandler[] handlers)
        {
            return KeenHttpClient.Create(
                baseUrl,
                httpClientProvider,
                KeenHttpClient.CreateHandlerChain(innerHandler, handlers)
                );
        }

        internal static string GetRelativeUrl(string projectId, string resource)
        {
            return $"projects/{projectId}/{resource}";
        }

        public Task<HttpResponseMessage> GetAsync(string resource, string authKey)
        {
            var url = new Uri(resource, UriKind.Relative);

            return GetAsync(url, authKey);
        }

        public Task<HttpResponseMessage> GetAsync(Uri resource, string authKey)
        {
            KeenHttpClient.RequireAuthKey(authKey);

            HttpRequestMessage get = KeenHttpClient.NewGet(resource, authKey);

            return _httpClient.SendAsync(get);
        }

        public Task<HttpResponseMessage> PostAsync(string resource, string authKey, string content)
        {
            var url = new Uri(resource, UriKind.Relative);

            return PostAsync(url, authKey, content);
        }

        // TODO : instead of (or in addition to) string, also accept HttpContent content or JObject content?
        public async Task<HttpResponseMessage> PostAsync(Uri resource, // TODO : Ensure this is provided
                                                   string authKey, // TODO : Ensure this is provided
                                                   string content) // TODO : Ensure this isn't null
        {
            KeenHttpClient.RequireAuthKey(authKey);

            if (string.IsNullOrWhiteSpace(content))
            {
                // Technically, we can encode an empty string or whitespace, but why? For now
                // we use GET for querying. If we ever need to POST with no content, we should
                // reorganize the logic below to never create/set the content stream.
                throw new ArgumentNullException(nameof(content), "Unexpected empty content.");
            }

            // If we switch PCL profiles, instead use MediaTypeFormatters (or ObjectContent<T>)?,
            // like here?: https://msdn.microsoft.com/en-us/library/system.net.http.httpclientextensions.putasjsonasync(v=vs.118).aspx
            using (var contentStream =
                new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                // TODO : Amake sure this is the same as Add("content-type", "application/json")
                contentStream.Headers.ContentType =
                    new MediaTypeHeaderValue(KeenHttpClient.JSON_CONTENT_TYPE);

                HttpRequestMessage post = KeenHttpClient.NewPost(resource, authKey);
                post.Content = contentStream;

                return await _httpClient.SendAsync(post).ConfigureAwait(false);

                // TODO : Should we do the KeenUtil.CheckApiErrorCode() here?
                // TODO : Should we check the if (!responseMsg.IsSuccessStatusCode) here too?
                // TODO : If we centralize error checking in this class we could have variations
                //     of these helpers that return string or JToken or JArray or JObject. It might
                //     also be nice for those options to optionally hand back the raw
                //     HttpResponseMessage in an out param if desired?
                // TODO : Use CallerMemberNameAttribute to print error messages?
                //     http://stackoverflow.com/questions/3095696/how-do-i-get-the-calling-method-name-and-type-using-reflection?noredirect=1&lq=1
            }
        }

        public Task<HttpResponseMessage> DeleteAsync(string resource, string authKey)
        {
            var url = new Uri(resource, UriKind.Relative);

            return DeleteAsync(url, authKey);
        }

        public Task<HttpResponseMessage> DeleteAsync(Uri resource, string authKey)
        {
            KeenHttpClient.RequireAuthKey(authKey);

            HttpRequestMessage delete = KeenHttpClient.NewDelete(resource, authKey);

            return _httpClient.SendAsync(delete);
        }

        private static HttpRequestMessage NewGet(Uri resource, string authKey)
        {
            return CreateRequest(HttpMethod.Get, resource, authKey);
        }

        private static HttpRequestMessage NewPost(Uri resource, string authKey)
        {
            return CreateRequest(HttpMethod.Post, resource, authKey);
        }

        private static HttpRequestMessage NewDelete(Uri resource, string authKey)
        {
            return CreateRequest(HttpMethod.Delete, resource, authKey);
        }

        private static HttpRequestMessage CreateRequest(HttpMethod verb,
                                                        Uri resource,
                                                        string authKey)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = resource,
                Method = verb
            };

            request.Headers.Add(KeenHttpClient.AUTH_HEADER_KEY, authKey);

            return request;
        }

        private static void RequireAuthKey(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new ArgumentNullException(nameof(authKey), "Auth key is required.");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;


namespace Keen.Core
{
    // Singleton as cache with WeakRef values in the dictionary

    /// <summary>
    /// TODO : Fill in
    /// </summary>
    internal class HttpClientCache : IHttpClientProvider
    {
        internal static HttpClientCache Instance { get; } = new HttpClientCache();

        // Explicit static constructor, no beforefieldinit
        static HttpClientCache() { }


        private readonly object _cacheLock;

        // NOTE : We should use ConcurrentDictionary<Uri, Lazy<>> here. if/when we upgrade the PCL
        // profile to something >= .NET 4.0.

        // NOTE : Use WeakReference<T> in 4.5+
        private readonly IDictionary<Uri, WeakReference> _httpClients;

        // No external construction
        internal HttpClientCache()
        {
            _cacheLock = new object();
            _httpClients = new Dictionary<Uri, WeakReference>();
        }


        // TODO : Should we have a minimum time before evicting? If you go about creating and
        // destroying KeenClient instances all the time, you could still see all references to
        // a given HttpClient go away, and the GC might be quick to collect and finalize before
        // the next KeenClient instance is created. This is bad usage of KeenClient, but doesn't
        // mean it couldn't happen. In that case, using GCHandle (which is lighter-weight anyway)
        // might be more appropriate, then implementing some form of timeout/GC keep-alive?
        // e.g. - https://www.codeproject.com/articles/35152/weakreferences-as-a-good-caching-mechanism

        // TODO : Maybe use Uri.IsBaseOf() to allow passing in any URI and getting the HttpClient
        // for a common base? So if we install an HttpClient for www.stuff.com, then
        // www.stuff.com/api/query and www.stuff.com/api/events would yield the same HttpClient.
        // But if you install one for /api/query and one for /api/events you could pass in
        // something like www.stuff.com/api/events/thing and get the events-specific HttpClient.
        // It would probably be best to do this check after normal dictionary lookup in case client
        // code passes the exact url, since it'll be faster.
        // Maybe strip path/query/fragment stuff as such?:
        //     baseUrl.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped)
        // Also include localPath? https://msdn.microsoft.com/en-us/library/7767559y(v=vs.110).aspx
        // If we do this, change it to not be "baseUrl" but rather just "url"


        /// <summary>
        /// TODO : Fill in
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public HttpClient this[Uri baseUrl]
        {
            get
            {
                HttpClient httpClient = null;

                lock (_cacheLock)
                {
                    WeakReference weakRef = null;

                    if (!_httpClients.TryGetValue(baseUrl, out weakRef))
                    {
                        throw new KeenException(
                            string.Format("No existing HttpClient for baseUrl \"{0}\"", baseUrl));
                    }

                    httpClient = weakRef.Target as HttpClient;

                    if (null == httpClient)
                    {
                        throw new KeenException(
                            string.Format("Existing HttpClient for baseUrl \"{0}\" has been" + 
                                          "garbage collected.", baseUrl));
                    }
                }

                return httpClient;
            }
        }

        /// <summary>
        /// Given the base URL, retrieve a cached HttpClient or create one. This takes a lock,
        /// so be wary if calling frequently from multiple threads.
        /// </summary>
        /// 
        /// <param name="baseUrl">The baseUrl for this HttpClient.</param>
        /// <param name="handlerChain">TODO : Fill in</param>
        /// <param name="defaultHeaders">Any headers that all requests to this URL should add by
        ///     default.</param>
        /// <returns>An HttpClient configured to handle requests for the given URL.</returns>
        public HttpClient GetOrCreateForUrl(
            Uri baseUrl,
            HttpMessageHandler handlerChain = null,
            IEnumerable<KeyValuePair<string, string>> defaultHeaders = null
            )
        {
            Action<HttpClient> configure = null;

            if (null != defaultHeaders && Enumerable.Any(defaultHeaders))
            {
                configure = (httpClientToConfigure) =>
                {
                    foreach (var header in defaultHeaders)
                    {
                        httpClientToConfigure.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                };
            }


            HttpClient httpClient = GetOrCreateForUrl(baseUrl, handlerChain, configure);

            return httpClient;
        }

        /// <summary>
        /// TODO : Fill in
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="handlerChain"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public HttpClient GetOrCreateForUrl(
            Uri baseUrl,
            HttpMessageHandler handlerChain = null,
            Action<HttpClient> configure = null
            )
        {
            if (null == baseUrl)
            {
                throw new ArgumentNullException(nameof(baseUrl),
                    string.Format("Cannot use a null {0} as a key.", nameof(baseUrl)));
            }

            HttpClient httpClient = null;

            lock (_cacheLock)
            {
                WeakReference weakRef = null;

                if (!_httpClients.TryGetValue(baseUrl, out weakRef) ||
                    null == (httpClient = weakRef.Target as HttpClient))
                {
                    // If no handler chain is provided, a plain HttpClientHandler with no
                    // configuration whatsoever is installed.
                    httpClient = new HttpClient(handlerChain ?? new HttpClientHandler());
                    httpClient.BaseAddress = baseUrl;

                    configure?.Invoke(httpClient);

                    // Reuse the WeakReference if we already had an entry for this url.
                    if (null == weakRef)
                    {
                        _httpClients[baseUrl] = new WeakReference(httpClient);
                    }
                    else
                    {
                        weakRef.Target = httpClient;
                    }
                }
            }

            return httpClient;
        }

        /// <summary>
        /// TODO : Fill in
        /// </summary>
        public void Clear()
        {
            lock (_cacheLock)
            {
                _httpClients.Clear();
            }
        }

        /// <summary>
        /// TODO : Fill in
        /// </summary>
        /// <param name="baseUrl"></param>
        public void RemoveForUrl(Uri baseUrl)
        {
            if (null == baseUrl)
            {
                throw new ArgumentNullException(nameof(baseUrl),
                    string.Format("Cannot use a null {0} as a key.", nameof(baseUrl)));
            }

            lock (_cacheLock)
            {
                _httpClients.Remove(baseUrl);
            }
        }

        /// <summary>
        /// Override the HttpClient provided for a given URL. This is useful for testing.
        /// TODO : As we relax accessibility, this should remain internal, probably.
        /// </summary>
        /// <param name="baseUrl">URL to override.</param>
        /// <param name="httpClient">HttpClient instance that will do the overriding.</param>
        internal void OverrideForUrl(Uri baseUrl, HttpClient httpClient)
        {
            lock (_cacheLock)
            {
                WeakReference weakRef = null;

                if (!_httpClients.TryGetValue(baseUrl, out weakRef))
                {
                    _httpClients[baseUrl] = new WeakReference(httpClient);
                }
                else
                {
                    weakRef.Target = httpClient;
                }
            }
        }
    }
}

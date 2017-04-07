using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;


namespace Keen.Core
{
    // Cache with a WeakRef as Singleton
    internal class HttpClientCache2 : IHttpClientProvider
    {
        private static readonly object _instanceLock = new object();
        // Use WeakReference<T> in 4.5+
        private static readonly WeakReference _instanceRef = new WeakReference(new HttpClientCache2());


        // Explicit static constructor, no beforefieldinit
        static HttpClientCache2() { }

        // TODO : Make it clear that the idea is to hold on to this thing. Alternative is the dict could be of WeakReferences.
        internal static HttpClientCache2 Instance => GetStrongRef();

        private static HttpClientCache2 GetStrongRef()
        {
            HttpClientCache2 strongRef = _instanceRef.Target as HttpClientCache2;

            if (null == strongRef)
            {
                // The idea is that we don't access this very frequently, so a lock should be fine.
                lock (_instanceLock)
                {
                    strongRef = _instanceRef.Target as HttpClientCache2;

                    if (null == strongRef)
                    {
                        strongRef = new HttpClientCache2();
                        _instanceRef.Target = strongRef;
                    }
                }
            }

            return strongRef;
        }


        // I wish we could use ConcurrentDictionary<Uri, Lazy<>> here. We should do so if/when we
        // upgrade the PCL profile to something >= .NET 4.0.
        private readonly object _cacheLock;
        private readonly Dictionary<Uri, HttpClient> _httpClients;


        // No external construction
        private HttpClientCache2()
        {
            _cacheLock = new object();
            _httpClients = new Dictionary<Uri, HttpClient>();
        }


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
        // If I do this, change it to not be "baseUrl" but rather just "url"


        public HttpClient this[Uri baseUrl]
        {
            get
            {
                HttpClient httpClient = null;

                lock (_cacheLock)
                {
                    if (!_httpClients.TryGetValue(baseUrl, out httpClient))
                    {
                        throw new KeenException(
                            string.Format("No existing HttpClient for baseUrl \"{0}\"", baseUrl));
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
            // TODO : Reorganize
            Action<HttpClient> configure = (
                (null == defaultHeaders || !Enumerable.Any(defaultHeaders)) ?
                (Action<HttpClient>)null :
                (httpClientToConfigure) =>
                {
                    //defaultHeaders = (defaultHeaders
                    //    ?? Enumerable.Empty<KeyValuePair<string, string>>());
                     
                    foreach (var header in defaultHeaders)
                    {
                        httpClientToConfigure.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            );

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
                if (!_httpClients.TryGetValue(baseUrl, out httpClient))
                {
                    // If no handler chain is provided, a plain HttpClientHandler with no
                    // configuration whatsoever is installed.
                    httpClient = new HttpClient(handlerChain ?? new HttpClientHandler());
                    httpClient.BaseAddress = baseUrl;

                    configure?.Invoke(httpClient);

                    _httpClients[baseUrl] = httpClient;
                }
            }

            return httpClient;
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
            lock(_cacheLock)
            {
                _httpClients[baseUrl] = httpClient;
            }
        }
    }
}

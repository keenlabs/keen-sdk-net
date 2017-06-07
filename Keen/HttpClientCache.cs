using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;


namespace Keen.Core
{
    /// <summary>
    /// An implementation of <see cref="IHttpClientProvider"/> that caches HttpClient instances in
    /// a dictionary mapping the base URL to a WeakReference to the actual instance. A PRO to this
    /// approach is that HttpClient instances will automatically be evicted when no more strong
    /// refs exist and the GC collects. A CON to using WeakReference, besides it not being generic
    /// in the current version of the PCL and being a fairly heavyweight class, is that rapid 
    /// creation and releasing of owning instances like the KeenClient can still allow for the GC
    /// to aggressively clean up HttpClient instances. Recommended usage of KeenClient shouldn't
    /// make this a common problem, but at some point this cache can evolve to be more intelligent
    /// about keeping instances alive deliberately.
    /// </summary>
    internal class HttpClientCache : IHttpClientProvider
    {
        // A singleton cache that can optionally be used and shared. If new caches need to be
        // created, use the internal ctor. One use case for this might be to have multiple
        // HttpClient instances with different configurations cached for the same URL for use in
        // different sets of client modules.
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


        /// <summary>
        /// Retrieve an existing HttpClient for the given URL, or throw if it doesn't exist.
        /// </summary>
        /// <param name="baseUrl">The base URL the HttpClient is tied to.</param>
        /// <returns>The HttpClient which is expected to exist.</returns>
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
        /// Retrieve an existing HttpClient for the given URL, or create one with the given
        /// handlers and headers.
        /// </summary>
        /// <param name="baseUrl">The base URL the HttpClient is tied to.</param>
        /// <param name="getHandlerChain">A factory function to create a handler chain.</param>
        /// <param name="defaultHeaders">Any headers that all requests to this URL should add by
        ///     default.</param>
        /// <returns>An HttpClient configured to handle requests for the given URL.</returns>
        public HttpClient GetOrCreateForUrl(
            Uri baseUrl,
            Func<HttpMessageHandler> getHandlerChain = null,
            IEnumerable<KeyValuePair<string, string>> defaultHeaders = null)
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


            HttpClient httpClient = GetOrCreateForUrl(baseUrl, getHandlerChain, configure);

            return httpClient;
        }

        /// <summary>
        /// Retrieve an existing HttpClient for the given URL, or create one with the given
        /// handlers and configuration functor.
        /// </summary>
        /// <param name="baseUrl">The base URL the HttpClient is tied to.</param>
        /// <param name="getHandlerChain">A factory function to create a handler chain.</param>
        /// <param name="configure">An action that takes the newly created HttpClient and
        ///     configures it however needed before it is stored and/or returned.</param>
        /// <returns>An HttpClient configured to handle requests for the given URL.</returns>
        public HttpClient GetOrCreateForUrl(
            Uri baseUrl,
            Func<HttpMessageHandler> getHandlerChain = null,
            Action<HttpClient> configure = null)
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
                    httpClient = new HttpClient(getHandlerChain?.Invoke() ?? new HttpClientHandler());
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
        /// Remove any cached HttpClients associated with the given URL.
        /// </summary>
        /// <param name="baseUrl">The base URL for which any cached HttpClient instances should
        ///     be purged.</param>
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
        /// Can this provider return an HttpClient instance for the given URL? For this
        /// implementation, we'll check if an entry exists in the cache and if the WeakReference
        /// is still valid and a strong ref can be taken.
        /// </summary>
        /// <param name="baseUrl">The base URL for which we'd like to know if an HttpClient can be
        ///     provided.</param>
        /// <returns>True if this provider could return an HttpClient for the given URL, false
        ///     otherwise.</returns>
        public bool ExistsForUrl(Uri baseUrl)
        {
            if (null == baseUrl)
            {
                // We can't have null keys, so we wouldn't ever be able to look up this URL.
                return false;
            }

            lock (_cacheLock)
            {
                WeakReference weakRef = null;
                bool exists = (_httpClients.TryGetValue(baseUrl, out weakRef) &&
                              (null != weakRef.Target as HttpClient));

                return exists;
            }
        }

        /// <summary>
        /// Drop all HttpClient instances from the cache, no matter the URL.
        /// </summary>
        internal void Clear()
        {
            lock (_cacheLock)
            {
                _httpClients.Clear();
            }
        }

        /// <summary>
        /// Override the HttpClient provided for a given URL. This tests and replaces or inserts
        /// all in one atomic operation. This will likely be useful for testing.
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

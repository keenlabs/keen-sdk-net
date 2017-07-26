using System;
using System.Collections.Generic;
using System.Net.Http;


namespace Keen.NetStandard
{
    /// <summary>
    /// Represents a type that can provide an HttpClient for a given URL. It could act as a cache
    /// by returning pre-existing instances, or create as necessary, or always create given the
    /// optional configuration parameters pass in.
    /// </summary>
    public interface IHttpClientProvider
    {
        /// <summary>
        /// Retrieve an existing HttpClient for the given URL.
        /// </summary>
        /// <param name="baseUrl">The base URL the HttpClient is tied to.</param>
        /// <returns>The HttpClient which is expected to exist.</returns>
        HttpClient this[Uri baseUrl] { get; }

        /// <summary>
        /// Retrieve an existing HttpClient for the given URL, or create one with the given
        /// handlers and configuration functor.
        /// </summary>
        /// <param name="baseUrl">The base URL the HttpClient is tied to.</param>
        /// <param name="getHandlerChain">A factory function to create a handler chain.</param>
        /// <param name="defaultHeaders">Any headers that all requests to this URL should add by
        ///     default.</param>
        /// <returns>An HttpClient configured to handle requests for the given URL.</returns>
        HttpClient GetOrCreateForUrl(
            Uri baseUrl,
            Func<HttpMessageHandler> getHandlerChain = null,
            IEnumerable<KeyValuePair<string, string>> defaultHeaders = null
        );

        /// <summary>
        /// Retrieve an existing HttpClient for the given URL, or create one with the given
        /// handlers and configuration functor.
        /// </summary>
        /// <param name="baseUrl">The base URL the HttpClient is tied to.</param>
        /// <param name="getHandlerChain">A factory function to create a handler chain.</param>
        /// <param name="configure">An action that takes the newly created HttpClient and
        ///     configures it however needed before it is stored and/or returned.</param>
        /// <returns>An HttpClient configured to handle requests for the given URL.</returns>
        HttpClient GetOrCreateForUrl(Uri baseUrl,
                                     Func<HttpMessageHandler> getHandlerChain = null,
                                     Action<HttpClient> configure = null);

        /// <summary>
        /// If caching instances, remove any associated with the given URL.
        /// </summary>
        /// <param name="baseUrl">The base URL for which any cached HttpClient instances should
        ///     be purged.</param>
        void RemoveForUrl(Uri baseUrl);

        /// <summary>
        /// Can this provider return an HttpClient instance for the given URL?
        /// </summary>
        /// <param name="baseUrl">The base URL for which we'd like to know if an HttpClient can be
        ///     provided.</param>
        /// <returns>True if this provider could return an HttpClient for the given URL, false
        ///     otherwise.</returns>
        bool ExistsForUrl(Uri baseUrl);
    }
}
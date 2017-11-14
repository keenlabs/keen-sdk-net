using System;


namespace Keen.Core
{
    /// <summary>
    /// An implementation of <see cref="IKeenHttpClientProvider"/> that uses the default
    /// <see cref="KeenHttpClient"/> creation logic and relies on the <see cref="HttpClientCache"/>
    /// class as an <see cref="IHttpClientProvider"/>.
    /// </summary>
    internal class KeenHttpClientProvider : IKeenHttpClientProvider
    {
        /// <summary>
        /// Given a base URL, return an IKeenHttpClient against which requests can be made.
        /// </summary>
        /// <param name="baseUrl">The base URL, e.g. https://api.keen.io/3.0/ </param>
        /// <returns>An IKeenHttpClient configured to handle requests to resources relative to the
        ///     given base URL.</returns>
        public IKeenHttpClient GetForUrl(Uri baseUrl)
        {
            var keenHttpClient = KeenHttpClientFactory.Create(baseUrl, HttpClientCache.Instance);

            return keenHttpClient;
        }
    }
}

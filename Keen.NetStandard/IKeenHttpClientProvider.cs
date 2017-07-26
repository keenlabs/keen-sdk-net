using System;


namespace Keen.NetStandard
{
    /// <summary>
    /// An instance of this type can provide an <see cref="IKeenHttpClient"/> to be used to perform
    /// requests against a given URL. Implement to customize how other parts of the SDK dispatch
    /// requests to a keen IO endpoint.
    /// </summary>
    public interface IKeenHttpClientProvider
    {
        /// <summary>
        /// Given a base URL, return an IKeenHttpClient against which requests can be made. The
        /// intent is that all requests using this IKeenHttpClient will be to resources relative to
        /// this base URL. It is expected that this IKeenHttpClient is thread-safe.
        /// </summary>
        /// <param name="baseUrl">The base URL, e.g. https://api.keen.io/3.0/ </param>
        /// <returns>An IKeenHttpClient configured to handle requests to resources relative to the
        ///     given base URL.</returns>
        IKeenHttpClient GetForUrl(Uri baseUrl);
    }
}

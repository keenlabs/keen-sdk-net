using System;


namespace Keen.Core.Test
{
    /// <summary>
    /// An implementation of <see cref="IKeenHttpClientProvider"/> that behaves just like
    /// <see cref="KeenHttpClientProvider"/> by default, but allows for easily overriding this
    /// behavior with a Func<> for use in tests.
    /// </summary>
    internal class TestKeenHttpClientProvider : IKeenHttpClientProvider
    {
        internal Func<Uri, IKeenHttpClient> ProvideKeenHttpClient =
            (url) => KeenHttpClientFactory.Create(url, HttpClientCache.Instance);


        public IKeenHttpClient GetForUrl(Uri baseUrl)
        {
            return ProvideKeenHttpClient(baseUrl);
        }
    }
}

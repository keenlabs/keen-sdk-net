using System;


namespace Keen.Core
{
    /// <summary>
    /// TODO : Fill in comments in this file.
    /// </summary>
    internal class KeenHttpClientProvider : IKeenHttpClientProvider
    {
        public IKeenHttpClient GetForUrl(Uri baseUrl)
        {
            var keenHttpClient = KeenHttpClient.Create(baseUrl, HttpClientCache.Instance);

            return keenHttpClient;
        }
    }
}

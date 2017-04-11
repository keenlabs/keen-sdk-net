using System;


namespace Keen.Core
{
    /// <summary>
    /// TODO : Fill in comments in this file.
    /// </summary>
    public interface IKeenHttpClientProvider
    {
        IKeenHttpClient GetForUrl(Uri baseUrl);
    }
}

using System;


namespace Keen.Core
{
    /// <summary>
    /// TODO : Fill in comments in this file.
    /// </summary>
    internal interface IKeenHttpClientProvider
    {
        IKeenHttpClient GetForUrl(Uri baseUrl);
    }
}

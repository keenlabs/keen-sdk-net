using System;
using System.Collections.Generic;
using System.Net.Http;


namespace Keen.Core
{
    /// <summary>
    /// TODO : Fill in comments in this file.
    /// </summary>
    internal interface IHttpClientProvider
    {
        HttpClient this[Uri baseUrl] { get; }

        HttpClient GetOrCreateForUrl(
            Uri baseUrl,
            HttpMessageHandler handlerChain = null,
            IEnumerable<KeyValuePair<string, string>> defaultHeaders = null
        );

        HttpClient GetOrCreateForUrl(Uri baseUrl,
                                     HttpMessageHandler handlerChain = null,
                                     Action<HttpClient> configure = null);

        void RemoveForUrl(Uri baseUrl);

        // TODO : Should there be a way to check if a cache entry already exists for a given url?
        //     Could be useful in case one wants to assert or throw if it's expected that it
        //     does/doesn't already exist, since it needs to be configured correctly.

        // TODO : Should the GetOrCreate*() overloads return a value indicating whether the
        //     HttpClient was created or previously existed? Maybe via an out param?

        // TODO : Should Override() be exposed, or some other way to force update? What if you 
        //    end up wanting two HttpClients for the same URL with different handler chains
        //    installed? What if you want to use a Proxy from one KeenClient but not for another
        //    KeenClient instance, but they hit the same URLs for the same exact project? You
        //    really need to have the cache map a URL to a collection of entries that each has
        //    some form of key.
    }
}
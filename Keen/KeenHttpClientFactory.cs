using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// A set of factory methods to help in creating see cref="IKeenHttpClient"/> instances. These
    /// are useful when implementing see cref="IKeenHttpClientProvider"/> so that the constructed
    /// instances have the right mix of default and custom configuration.
    /// </summary>
    public static class KeenHttpClientFactory
    {
        private static readonly IEnumerable<KeyValuePair<string, string>> DEFAULT_HEADERS =
            new[] { new KeyValuePair<string, string>("Keen-Sdk", KeenUtil.GetSdkVersion()) };


        private class LoggingHttpHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                // TODO : Log stuff before and after request, then move to its own file.

                // Now dispatch to the inner handler via the base impl.
                return base.SendAsync(request, cancellationToken);
            }
        }


        private static HttpMessageHandler CreateHandlerChainInternal(
            HttpClientHandler innerHandler,
            IEnumerable<DelegatingHandler> handlers)
        {
            // NOTE : There is no WebProxy available to the PCL profile, so we have to create an
            // IWebProxy implementation manually. Proxy is only supported on HttpClientHandler, and
            // not directly on DelegatingHandler, so handle that too. Basically only support Proxy
            // if client code does *not* give us an HttpClientHandler. Or else set the Proxy on 
            // their handler, but make sure it's not already set.

            // Example of how setting Proxy works in big .NET where the WebProxy class exists:
            //
            // new HttpClientHandler()
            // {
            //     Proxy = WebProxy("http://localhost:8888", false),
            //     UseProxy = true
            // };


            // TODO : Also, to support Proxy, we have to realize we'd be turning it on for a given
            // HttpClientHandler already installed for the HttpClient in the cache for a given URL.
            // Since modifications aren't allowed for HttpClients/*Handlers, we would replace the 
            // HttpClient, which would affect all future users of the cache requesting an
            // HttpClient for that URL, when really we want an abstraction to keep that sort of
            // setting tied to, which maybe is this KeenHttpClient?


            HttpMessageHandler handlerChain = (innerHandler ?? new HttpClientHandler());

            if (null == handlers)
            {
                return handlerChain;
            }

            foreach (var handler in handlers.Reverse())
            {
                if (null == handler)
                {
                    throw new ArgumentNullException(nameof(handlers),
                        "One of the given DelegatingHandler params was null.");
                }

                if (null != handler.InnerHandler)
                {
                    throw new ArgumentException("Encountered a non-null InnerHandler in handler " +
                                                "chain, which would be overwritten.",
                                                nameof(handlers));
                }

                // This will throw if the given handler has already started any requests.
                // Basically all properties on all HttpClient/*Handler variations call
                // CheckDisposedOrStarted() in any setter, so the entire HttpClient is pretty much
                // locked down once it starts getting used.
                handler.InnerHandler = handlerChain;
                handlerChain = handler;
            }

            return handlerChain;
        }

        private static IEnumerable<DelegatingHandler> CreateDefaultDelegatingHandlers()
        {
            // TODO : Put more custom handlers in here, like retry/failure/proxy/logging handlers.

            // Create these every time, since *Handlers can't have properties changed after they've
            // started handling requests for an HttpClient.
            return new[] { new LoggingHttpHandler() };
        }

        /// <summary>
        /// Create the default handler pipeline with only Keen internal handlers installed.
        /// </summary>
        /// returns>The default handler chain.</returns>
        public static HttpMessageHandler CreateDefaultHandlerChain()
        {
            return KeenHttpClientFactory.CreateHandlerChainInternal(
                null,
                KeenHttpClientFactory.CreateDefaultDelegatingHandlers());
        }

        /// <summary>
        /// Create an HttpMessageHandler representing the handler pipeline. We will construct the
        /// HTTP handler pipeline such that provided handlers are called in order for requests, and
        /// receive responses in reverse order. Keen internal handlers will defer to the first
        /// DelegatingHandler and the pipeline will terminate at our HttpClientHandler.
        /// </summary>
        /// <param name="handlers">Handlers to be chained in the pipeline.</param>
        /// returns>The entire handler chain.</returns>
        public static HttpMessageHandler CreateHandlerChain(params DelegatingHandler[] handlers)
        {
            return KeenHttpClientFactory.CreateHandlerChain(null, handlers);
        }

        /// <summary>
        /// Create an HttpMessageHandler representing the handler pipeline. We will construct the
        /// HTTP handler pipeline such that provided handlers are called in order for requests, and
        /// receive responses in reverse order. Keen internal handlers will defer to the first
        /// DelegatingHandler and the pipeline will terminate at our HttpClientHandler or to the
        /// given HttpClientHandler if present, in case client code wants to do something like use
        /// WebRequestHandler functionality or otherwise add custom behavior.
        /// </summary>
        /// <param name="innerHandler">Terminating HttpClientHandler.</param>
        /// <param name="handlers">Handlers to be chained in the pipeline.</param>
        /// <returns>The entire handler chain.</returns>
        public static HttpMessageHandler CreateHandlerChain(HttpClientHandler innerHandler,
                                                            params DelegatingHandler[] handlers)
        {
            // We put our handlers first. Client code can look at the final state of the request
            // this way. Overwriting built-in handler state is shooting oneself in the foot.
            IEnumerable<DelegatingHandler> intermediateHandlers =
                KeenHttpClientFactory.CreateDefaultDelegatingHandlers().Concat(handlers);

            return KeenHttpClientFactory.CreateHandlerChainInternal(innerHandler,
                                                                    intermediateHandlers);
        }

        // NOTE : BaseUrl should have a final slash or the last Uri part is discarded. Also,
        // relative urls can *not* start with a slash.

        // Not exposed so that 3rd party code doesn't accidentally build a KeenHttpClient without
        // our handlers installed, which wouldn't be ideal.
        private static KeenHttpClient Create(Uri baseUrl,
                                             IHttpClientProvider httpClientProvider,
                                             Func<HttpMessageHandler> getHandlerChain)
        {
            if (!baseUrl.IsAbsoluteUri)
            {
                throw new ArgumentException(
                    "The given base Url must be in the form of an absolute Uri.",
                    nameof(baseUrl));
            }

            // Delay actual creation of the handler chain by passing in a Func<> to create it. This
            // way if HttpClient already exists, we won't bother creating/modifying handlers.
            var httpClient = httpClientProvider.GetOrCreateForUrl(
                baseUrl,
                getHandlerChain,
                KeenHttpClientFactory.DEFAULT_HEADERS);

            var newClient = new KeenHttpClient(httpClient);

            return newClient;
        }

        /// <summary>
        /// Construct an IKeenHttpClient for the given base URL, configured with an HttpClient that
        /// is retrieved and/or stored in the given IHttpClientProvider. If necessary, the
        /// HttpClient is created and configured with the default set of HTTP handlers.
        /// 
        /// <seealso cref="KeenHttpClientFactory.CreateDefaultHandlerChain"/>
        /// 
        /// </summary>
        /// <param name="baseUrl">The base URL for the constructed IKeenHttpClient.</param>
        /// <param name="httpClientProvider">The provider used to retrieve the HttpClient.</param>
        /// <returns>A new IKeenHttpClient for the given base URL.</returns>
        public static IKeenHttpClient Create(Uri baseUrl, IHttpClientProvider httpClientProvider)
        {
            return KeenHttpClientFactory.Create(
                baseUrl,
                httpClientProvider,
                () => KeenHttpClientFactory.CreateDefaultHandlerChain());
        }

        /// <summary>
        /// Construct an IKeenHttpClient for the given base URL, configured with an HttpClient that
        /// is retrieved and/or stored in the given IHttpClientProvider, and if necessary,
        /// configured with the given HTTP handlers.
        /// 
        /// <seealso cref="KeenHttpClientFactory.CreateHandlerChain"/>
        /// 
        /// </summary>
        /// <param name="baseUrl">The base URL for the constructed IKeenHttpClient.</param>
        /// <param name="httpClientProvider">The provider used to retrieve the HttpClient.</param>
        /// <param name="innerHandler">HTTP handler terminating the handler chain.</param>
        /// <param name="handlers">Handlers to be chained in the pipeline.</param>
        /// <returns>A new IKeenHttpClient for the given base URL.</returns>
        public static IKeenHttpClient Create(Uri baseUrl,
                                             IHttpClientProvider httpClientProvider,
                                             HttpClientHandler innerHandler,
                                             params DelegatingHandler[] handlers)
        {
            return KeenHttpClientFactory.Create(
                baseUrl,
                httpClientProvider,
                () => KeenHttpClientFactory.CreateHandlerChain(innerHandler, handlers));
        }

        /// <summary>
        /// Construct an IKeenHttpClient for the given base URL, configured with an HttpClient that
        /// is retrieved and/or stored in the given IHttpClientProvider, and if necessary,
        /// configured with the given HTTP handlers in a lazy fashion only if construction is
        /// necessary. Note that the given handler factory function could be called under a lock,
        /// so care should be taken in multi-threaded scenarios.
        /// 
        /// <seealso cref="KeenHttpClientFactory.CreateHandlerChain"/>
        /// 
        /// </summary>
        /// <param name="baseUrl">The base URL for the constructed IKeenHttpClient.</param>
        /// <param name="httpClientProvider">The provider used to retrieve the HttpClient.</param>
        /// <param name="getHandlers">A factory function called if construction of the HttpClient
        ///     is necessary. It should return an optional HttpClientHandler to terminate the
        ///     handler chain, as well as an optional list of intermediate HTTP handlers to be
        ///     chained in the pipeline.</param>
        /// <returns>A new IKeenHttpClient for the given base URL.</returns>
        public static IKeenHttpClient Create(
            Uri baseUrl,
            IHttpClientProvider httpClientProvider,
            Func<Tuple<HttpClientHandler, IEnumerable<DelegatingHandler>>> getHandlers)
        {
            Func<HttpMessageHandler> getHandlerChain = () =>
            {
                Tuple<HttpClientHandler, IEnumerable<DelegatingHandler>> handlers =
                    getHandlers?.Invoke();

                return KeenHttpClientFactory.CreateHandlerChainInternal(handlers.Item1,
                                                                        handlers.Item2);
            };

            return KeenHttpClientFactory.Create(
                baseUrl,
                httpClientProvider,
                getHandlerChain);
        }
    }
}

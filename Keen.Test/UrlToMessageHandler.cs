using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.Test
{
    /// <summary>
    /// An <see cref="IHttpMessageHandler"/> that matches request URIs with other
    /// IHttpMessageHandler instances. If configured as such, matching can be done such that when
    /// a precise match for a URL isn't found, any base URL entry will be used. This would mean one
    /// could match, say, any queries under a certain project ID without specifying all the
    /// query parameters, if that's useful. Another entry could match events, for example. By
    /// default, if no strict or loose match is found, this will try to forward to DefaultAsync, so
    /// either make sure that is set by a wrapper or explicitly, or configure to not defer to
    /// the default action.
    /// 
    /// This is handy to set up some validation and return canned responses for sets of URLs.
    /// 
    /// By sticking IHttpMessageHandler instances in this mapping, their DefaultAsync properties
    /// get forwarded to the DefaultAsync of this handler. That means if a match is found, and the
    /// handler used decides to call DefaultAsync, it will call this handler's default action,
    /// bypassing DeferToDefault, so make sure the contained handlers know whether or not to call
    /// default. TODO : Evaluate this behavior since maybe the defaults here are confusing.
    /// 
    /// </summary>
    internal class UrlToMessageHandler : IHttpMessageHandler
    {
        private readonly IDictionary<Uri, IHttpMessageHandler> _urlsToHandlers;

        public Func<HttpRequestMessage,
                    CancellationToken,
                    Task<HttpResponseMessage>> DefaultAsync
        { get; set; }

        internal bool DeferToDefault { get; set; } = true;

        internal bool MatchBaseUrls { get; set; } = true;

        internal UrlToMessageHandler(IDictionary<Uri, IHttpMessageHandler> urlsToHandlers)
        {
            _urlsToHandlers = new Dictionary<Uri, IHttpMessageHandler>(urlsToHandlers);

            foreach (var handler in _urlsToHandlers.Values)
            {
                // Lazily dispatch to whatever our default handler gets set to.
                // Be careful because trying to reuse handler instances will lead to strange
                // outcomes w.r.t. the default behavior of these IHttpMessageHandlers.
                handler.DefaultAsync = (request, ct) => DefaultAsync(request, ct);
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            IHttpMessageHandler handler = null;

            // First check for a perfect match, or if there's a key that's a base url of the request
            // url, match it if client code chooses to accept that.
            if (_urlsToHandlers.TryGetValue(request.RequestUri, out handler) ||
                (MatchBaseUrls && null != (handler = _urlsToHandlers.FirstOrDefault(
                    entry => entry.Key.IsBaseOf(request.RequestUri)).Value)))
            {
                response =
                    await handler.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            else if (DeferToDefault)
            {
                response = await DefaultAsync(request, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Console.WriteLine(string.Format("WARNING: No validator found for absolute URI: {0}",
                                                request.RequestUri.AbsoluteUri));

                // No handler found, so return 404
                response = await HttpTests.CreateJsonStringResponseAsync(
                    HttpStatusCode.NotFound, "Resource not found.", "ResourceNotFoundError")
                    .ConfigureAwait(false);
            }

            return response;
        }
    }
}

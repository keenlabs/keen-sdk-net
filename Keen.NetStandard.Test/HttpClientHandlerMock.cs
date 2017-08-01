using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.NetStandard.Test
{
    /// <summary>
    /// Wraps an <see cref="IHttpMessageHandler"/> and allows for using it as an HttpClientHandler.
    /// If the IHttpMessageHandler doesn't already have a default action set, we'll have it call
    /// our own base SendAsync() which will forward the request to the actual HttpClientHandler
    /// implementation, with all the configuration and proxies and such, which may actually go out
    /// over the network.
    /// </summary>
    internal class HttpClientHandlerMock : HttpClientHandler
    {
        internal readonly IHttpMessageHandler _handler;

        internal HttpClientHandlerMock(IHttpMessageHandler handler)
        {
            _handler = handler;
            _handler.DefaultAsync = (_handler.DefaultAsync ?? base.SendAsync);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            return _handler.SendAsync(request, cancellationToken);
        }
    }
}

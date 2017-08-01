using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.NetStandard.Test
{
    /// <summary>
    /// Wraps an <see cref="IHttpMessageHandler"/> and allows for using it as a DelegatingHandler.
    /// If the IHttpMessageHandler doesn't already have a default action set, we'll have it call
    /// our own base SendAsync() which will forward the request down the handler chain.
    /// </summary>
    internal class DelegatingHandlerMock : DelegatingHandler
    {
        private readonly IHttpMessageHandler _handler;

        internal DelegatingHandlerMock(IHttpMessageHandler handler)
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

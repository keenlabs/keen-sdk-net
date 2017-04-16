using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.Net.Test
{
    /// <summary>
    /// An <see cref="IHttpMessageHandler"/> that has pre/post/default message handlers functors,
    /// as well as a Func<> that produces the actual HttpResponseMessage. These can all be set by
    /// test code and will be called if available. There are defaults in place that essentially do
    /// nothing, but client code should make sure DefaultAsync gets set, either by a wrapper or
    /// explicitly.
    /// </summary>
    internal class FuncHandler : IHttpMessageHandler
    {
        internal Action<HttpRequestMessage, CancellationToken> PreProcess = (request, ct) => { };

        internal Func<HttpRequestMessage,
                      CancellationToken,
                      Task<HttpResponseMessage>> ProduceResultAsync =
                          (request, ct) => Task.FromResult<HttpResponseMessage>(null);

        internal Func<HttpRequestMessage,
                      HttpResponseMessage,
                      HttpResponseMessage> PostProcess = (request, response) => response;

        internal bool DeferToDefault { get; set; } = true;

        public Func<HttpRequestMessage,
                    CancellationToken,
                    Task<HttpResponseMessage>> DefaultAsync { get; set; }


        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            PreProcess(request, cancellationToken);
            HttpResponseMessage response =
                await ProduceResultAsync(request, cancellationToken).ConfigureAwait(false);

            // Pass it along down the line if we didn't create a result already.
            if (null == response && DeferToDefault)
            {
                response = await DefaultAsync(request, cancellationToken).ConfigureAwait(false);
            }

            PostProcess(request, response);

            return response;
        }
    }
}

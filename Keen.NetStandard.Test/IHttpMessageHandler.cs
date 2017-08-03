using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Keen.NetStandard
{
    /// <summary>
    /// Represents the main functionality needed to override both HttpClientHandler and
    /// DelegatingHandler. This can be useful for implementing test code in pass-through fakes
    /// where we want to alter some behavior, but let the rest execute normally. If the test just
    /// tests/mutates and forwards to another handler, it can implement this interface and be used
    /// in place of either type of handler in tests.
    /// 
    /// It's a bad idea to reuse instances of this type, since the wrappers as well as the
    /// HttpClient and pipeline code mess with their properties. Weirdness ensues, so create a
    /// fresh instance every time at the point where you create the wrapper or stick it in the
    /// pipeline, and don't store a reference to it unless it's needed to check later in an
    /// assert or some verification logic, but generally don't reuse it.
    /// </summary>
    internal interface IHttpMessageHandler
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                            CancellationToken cancellationToken);

        Func<HttpRequestMessage,
             CancellationToken,
             Task<HttpResponseMessage>> DefaultAsync { get; set; }
    }
}

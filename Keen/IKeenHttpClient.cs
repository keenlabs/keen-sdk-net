using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// Represents a type capable of performing HTTP operations destined for a Keen API endpoint.
    /// This should augment and/or alter normal HttpClient behavior where appropriate taking into
    /// consideration Keen-specific protocols.
    /// </summary>
    public interface IKeenHttpClient
    {
        /// <summary>
        /// Create and send a GET request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to GET. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>>The response message.</returns>
        Task<HttpResponseMessage> GetAsync(string resource, string authKey);

        /// <summary>
        /// Create and send a GET request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to GET.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>>The response message.</returns>
        Task<HttpResponseMessage> GetAsync(Uri resource, string authKey);

        /// <summary>
        /// Create and send a POST request with the given content to the given relative resource
        /// using the given key for authentication. 
        /// </summary>
        /// <param name="resource">The relative resource to GET. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <param name="content">The POST body to send.</param>
        /// <returns>>The response message.</returns>
        Task<HttpResponseMessage> PostAsync(string resource, string authKey, string content);

        /// <summary>
        /// Create and send a POST request with the given content to the given relative resource
        /// using the given key for authentication. 
        /// </summary>
        /// <param name="resource">The relative resource to GET.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <param name="content">The POST body to send.</param>
        /// <returns>>The response message.</returns>
        Task<HttpResponseMessage> PostAsync(Uri resource, string authKey, string content);

        /// <summary>
        /// Create and send a DELETE request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to DELETE. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>The response message.</returns>
        Task<HttpResponseMessage> DeleteAsync(string resource, string authKey);

        /// <summary>
        /// Create and send a DELETE request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to DELETE.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>The response message.</returns>
        Task<HttpResponseMessage> DeleteAsync(Uri resource, string authKey);
    }
}

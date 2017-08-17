using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// Helps with performing HTTP operations destined for a Keen API endpoint. Helper methods in
    /// this class will add appropriate headers and config to use the underlying HttpClient
    /// in the way expected by the Keen IO API. This class should be long-lived and all public
    /// methods are thread-safe, so ideal usage is to configure it once for a given base URL and
    /// reuse it with relative resources to send requests for the duration of the app or module.
    /// </summary>
    internal class KeenHttpClient : IKeenHttpClient
    {
        private static readonly string JSON_CONTENT_TYPE = "application/json";
        private static readonly string AUTH_HEADER_KEY = "Authorization";


        // We don't destroy this manually. Whatever code provides the HttpClient directly or via an
        // IHttpClientProvider should be sure to handle its lifetime.
        private readonly HttpClient _httpClient = null;


        internal KeenHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        internal static string GetRelativeUrl(string projectId, string resource)
        {
            return $"projects/{projectId}/{resource}";
        }

        /// <summary>
        /// Create and send a GET request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to GET. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>>The response message.</returns>
        public Task<HttpResponseMessage> GetAsync(string resource, string authKey)
        {
            var url = new Uri(resource, UriKind.Relative);

            return GetAsync(url, authKey);
        }

        /// <summary>
        /// Create and send a GET request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to GET.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>>The response message.</returns>
        public Task<HttpResponseMessage> GetAsync(Uri resource, string authKey)
        {
            KeenHttpClient.RequireAuthKey(authKey);

            HttpRequestMessage get = CreateRequest(HttpMethod.Get, resource, authKey);

            return _httpClient.SendAsync(get);
        }

        // TODO : Instead of (or in addition to) string, also accept HttpContent content and/or
        // JObject content?

        /// <summary>
        /// Create and send a POST request with the given content to the given relative resource
        /// using the given key for authentication. 
        /// </summary>
        /// <param name="resource">The relative resource to POST. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <param name="content">The POST body to send.</param>
        /// <returns>>The response message.</returns>
        public Task<HttpResponseMessage> PostAsync(string resource, string authKey, string content)
        {
            var url = new Uri(resource, UriKind.Relative);

            return PostAsync(url, authKey, content);
        }

        /// <summary>
        /// Create and send a POST request with the given content to the given relative resource
        /// using the given key for authentication. 
        /// </summary>
        /// <param name="resource">The relative resource to POST.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <param name="content">The POST body to send.</param>
        /// <returns>>The response message.</returns>
        public async Task<HttpResponseMessage> PostAsync(Uri resource,
                                                         string authKey,
                                                         string content)
        {
            KeenHttpClient.RequireAuthKey(authKey);

            if (string.IsNullOrWhiteSpace(content))
            {
                // Technically, we can encode an empty string or whitespace, but why? For now
                // we use GET for querying. If we ever need to POST with no content, we should
                // reorganize the logic below to never create/set the content stream.
                throw new ArgumentNullException(nameof(content), "Unexpected empty content.");
            }

            // If we switch PCL profiles, instead use MediaTypeFormatters (or ObjectContent<T>)?,
            // like here?: https://msdn.microsoft.com/en-us/library/system.net.http.httpclientextensions.putasjsonasync(v=vs.118).aspx
            using (var contentStream =
                new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                // TODO : Amake sure this is the same as Add("content-type", "application/json")
                contentStream.Headers.ContentType =
                    new MediaTypeHeaderValue(KeenHttpClient.JSON_CONTENT_TYPE);

                HttpRequestMessage post = CreateRequest(HttpMethod.Post, resource, authKey);
                post.Content = contentStream;

                return await _httpClient.SendAsync(post).ConfigureAwait(false);

                // TODO : Should we do the KeenUtil.CheckApiErrorCode() here?
                // TODO : Should we check the if (!responseMsg.IsSuccessStatusCode) here too?
                // TODO : If we centralize error checking in this class we could have variations
                //     of these helpers that return string or JToken or JArray or JObject. It might
                //     also be nice for those options to optionally hand back the raw
                //     HttpResponseMessage in an out param if desired?
                // TODO : Use CallerMemberNameAttribute to print error messages?
                //     http://stackoverflow.com/questions/3095696/how-do-i-get-the-calling-method-name-and-type-using-reflection?noredirect=1&lq=1
            }
        }

        /// <summary>
        /// Create and send a DELETE request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to DELETE. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>The response message.</returns>
        public Task<HttpResponseMessage> DeleteAsync(string resource, string authKey)
        {
            var url = new Uri(resource, UriKind.Relative);

            return DeleteAsync(url, authKey);
        }

        /// <summary>
        /// Create and send a DELETE request to the given relative resource using the given key for
        /// authentication.
        /// </summary>
        /// <param name="resource">The relative resource to DELETE.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <returns>The response message.</returns>
        public Task<HttpResponseMessage> DeleteAsync(Uri resource, string authKey)
        {
            KeenHttpClient.RequireAuthKey(authKey);

            HttpRequestMessage delete = CreateRequest(HttpMethod.Delete, resource, authKey);

            return _httpClient.SendAsync(delete);
        }

        /// <summary>
        /// Create and send a PUT request with the given content to the given relative resource
        /// using the given key for authentication. 
        /// </summary>
        /// <param name="resource">The relative resource to PUT. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <param name="content">The PUT body to send.</param>
        /// <returns>>The response message.</returns>
        public Task<HttpResponseMessage> PutAsync(string resource, string authKey, string content)
        {
            var url = new Uri(resource, UriKind.Relative);

            return PutAsync(url, authKey, content);
        }

        /// <summary>
        /// Create and send a PUT request with the given content to the given relative resource
        /// using the given key for authentication. 
        /// </summary>
        /// <param name="resource">The relative resource to PUT. Must be properly formatted as a
        ///     relative Uri.</param>
        /// <param name="authKey">The key to use for authenticating this request.</param>
        /// <param name="content">The PUT body to send.</param>
        /// <returns>>The response message.</returns>
        public async Task<HttpResponseMessage> PutAsync(Uri resource, string authKey, string content)
        {
            KeenHttpClient.RequireAuthKey(authKey);

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentNullException(nameof(content), "Unexpected empty content.");
            }

            using (var contentStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                contentStream.Headers.ContentType = new MediaTypeHeaderValue(KeenHttpClient.JSON_CONTENT_TYPE);

                HttpRequestMessage put = CreateRequest(HttpMethod.Put, resource, authKey);
                put.Content = contentStream;

                return await _httpClient.SendAsync(put).ConfigureAwait(false);
            }
        }

        private static HttpRequestMessage CreateRequest(HttpMethod verb,
                                                        Uri resource,
                                                        string authKey)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = resource,
                Method = verb
            };

            request.Headers.Add(KeenHttpClient.AUTH_HEADER_KEY, authKey);

            return request;
        }

        private static void RequireAuthKey(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new ArgumentNullException(nameof(authKey), "Auth key is required.");
            }
        }
    }
}

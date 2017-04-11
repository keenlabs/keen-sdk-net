using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// TODO : Fill in comments in this file
    /// </summary>
    public interface IKeenHttpClient
    {
        Task<HttpResponseMessage> DeleteAsync(Uri resource, string authKey);
        Task<HttpResponseMessage> DeleteAsync(string resource, string authKey);
        Task<HttpResponseMessage> GetAsync(Uri resource, string authKey);
        Task<HttpResponseMessage> GetAsync(string resource, string authKey);
        Task<HttpResponseMessage> PostAsync(Uri resource, string authKey, string content);
        Task<HttpResponseMessage> PostAsync(string resource, string authKey, string content);
    }
}
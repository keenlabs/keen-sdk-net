using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// Event implements the IEvent interface which represents the Keen.IO Event API methods.
    /// </summary>
    internal class Event : IEvent
    {
        private IProjectSettings _prjSettings;
        private string _serverUrl;

        /// <summary>
        /// Get details of all schemas in the project.
        /// </summary>
        /// <returns></returns>
        public async Task<JArray> GetSchemas()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);
                client.DefaultRequestHeaders.Add("Keen-Sdk", KeenUtil.GetSdkVersion());

                var responseMsg = await client.GetAsync(_serverUrl)
                    .ConfigureAwait(continueOnCapturedContext: false);
                var responseString = await responseMsg.Content.ReadAsStringAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
                dynamic response = JArray.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode(response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("GetSchemas failed with status: " + responseMsg.StatusCode);

                return response;
            }
        }

        /// <summary>
        /// Add all events in a single request.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task<IEnumerable<CachedEvent>> AddEvents(JObject events)
        {
            var content = events.ToString();

            using (var client = new HttpClient())
            using (var contentStream = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))))
            {
                contentStream.Headers.Add("content-type", "application/json");

                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.WriteKey);
                client.DefaultRequestHeaders.Add("Keen-Sdk", KeenUtil.GetSdkVersion());

                var httpResponse = await client.PostAsync(_serverUrl, contentStream)
                    .ConfigureAwait(continueOnCapturedContext: false);
                var responseString = await httpResponse.Content.ReadAsStringAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
                JObject jsonResponse = null;
                try
                {
                    // Normally the response content should be parsable JSON,
                    // but if the server returned a 404 error page or something
                    // like that, this will throw. 
                    jsonResponse = JObject.Parse(responseString);
                }
                catch (Exception)
                { }

                if (!httpResponse.IsSuccessStatusCode)
                    throw new KeenException("AddEvents failed with status: " + httpResponse);
                if (null == jsonResponse)
                    throw new KeenException("AddEvents failed with empty response from server.");

                // error checking, return failed events in the list,
                // or if the HTTP response is a failure, throw.
                var failedItems = from respCols in jsonResponse.Properties()
                                  from eventsCols in events.Properties()
                                  where respCols.Name == eventsCols.Name
                                  let collection = respCols.Name
                                  let combined = eventsCols.Children().Children()
                                    .Zip(respCols.Children().Children(),
                                    (e, r) => new { eventObj = (JObject)e, result = (JObject)r })
                                  from e in combined
                                  where !(bool)(e.result.Property("success").Value)
                                  select new CachedEvent(collection, e.eventObj, KeenUtil.GetBulkApiError(e.result));

                return failedItems;
            }
        }

        public Event(IProjectSettings prjSettings)
        {
            _prjSettings = prjSettings;

            _serverUrl = string.Format("{0}projects/{1}/{2}",
                _prjSettings.KeenUrl, _prjSettings.ProjectId, KeenConstants.EventsResource);
        }

    }
}

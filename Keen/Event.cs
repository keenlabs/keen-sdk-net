using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Keen.Core
{
    /// <summary>
    /// Event implements the IEvent interface which represents the Keen.IO Event API methods.
    /// </summary>
    internal class Event : IEvent
    {
        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _eventsRelativeUrl;
        private readonly string _readKey;
        private readonly string _writeKey;


        public Event(IProjectSettings prjSettings,
                     IKeenHttpClientProvider keenHttpClientProvider)
        {
            if (null == prjSettings)
            {
                throw new ArgumentNullException(nameof(prjSettings),
                                                "Project Settings must be provided.");
            }

            if (null == keenHttpClientProvider)
            {
                throw new ArgumentNullException(nameof(keenHttpClientProvider),
                                                "A KeenHttpClient provider must be provided.");
            }

            if (string.IsNullOrWhiteSpace(prjSettings.KeenUrl) ||
                !Uri.IsWellFormedUriString(prjSettings.KeenUrl, UriKind.Absolute))
            {
                throw new KeenException(
                    "A properly formatted KeenUrl must be provided via Project Settings.");
            }

            var serverBaseUrl = new Uri(prjSettings.KeenUrl);
            _keenHttpClient = keenHttpClientProvider.GetForUrl(serverBaseUrl);
            _eventsRelativeUrl = KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
                                                               KeenConstants.EventsResource);

            _readKey = prjSettings.ReadKey;
            _writeKey = prjSettings.WriteKey;
        }

        public Event(IProjectSettings prjSettings)
            : this(prjSettings, new KeenHttpClientProvider())
        {
        }

        /// <summary>
        /// Get details of all schemas in the project.
        /// </summary>
        /// <returns></returns>
        public async Task<JArray> GetSchemas()
        {
            // TODO : Make sure read key is sufficient instead of master key...
            if (string.IsNullOrWhiteSpace(_readKey))
            {
                throw new KeenException("An API ReadKey is required to get schemas.");
            }

            var responseMsg = await _keenHttpClient
                .GetAsync(_eventsRelativeUrl, _readKey)
                .ConfigureAwait(continueOnCapturedContext: false);
            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
            var response = JArray.Parse(responseString);

            // error checking, throw an exception with information from the json 
            // response if available, then check the HTTP response.
            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("GetSchemas failed with status: " +
                                        responseMsg.StatusCode);
            }

            return response;
        }

        /// <summary>
        /// Add all events in a single request.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task<IEnumerable<CachedEvent>> AddEvents(JObject events)
        {
            if (string.IsNullOrWhiteSpace(_writeKey))
            {
                throw new KeenException("An API WriteKey is required to add events.");
            }

            var content = events.ToString();

            var responseMsg = await _keenHttpClient
                .PostAsync(_eventsRelativeUrl, _writeKey, content)
                .ConfigureAwait(continueOnCapturedContext: false);
            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            JObject jsonResponse = null;

            try
            {
                // Normally the response content should be parsable JSON,
                // but if the server returned a 404 error page or something
                // like that, this will throw. 
                jsonResponse = JObject.Parse(responseString);

                // TODO : Why do we not call KeenUtil.CheckApiErrorCode(jsonResponse); ??
            }
            catch (Exception)
            {
            }

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("AddEvents failed with status: " + responseMsg.StatusCode);
            }

            if (null == jsonResponse)
            {
                throw new KeenException("AddEvents failed with empty response from server.");
            }

            // error checking, return failed events in the list,
            // or if the HTTP response is a failure, throw.
            var failedItems =
                from respCols in jsonResponse.Properties()
                    from eventsCols in events.Properties()
                        where respCols.Name == eventsCols.Name
                            let collection = respCols.Name
                            let combined = eventsCols.Children().Children()
                                .Zip(respCols.Children().Children(),
                                     (e, r) => new { eventObj = (JObject)e, result = (JObject)r })
                                from e in combined
                                    where !(bool)(e.result.Property("success").Value)
                                    select new CachedEvent(collection,
                                                           e.eventObj,
                                                           KeenUtil.GetBulkApiError(e.result));

            return failedItems;
        }
    }
}

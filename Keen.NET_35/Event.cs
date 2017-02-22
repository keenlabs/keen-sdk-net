using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Keen.NET_35
{
    /// <summary>
    /// Event implements the IEvent interface which represents the Keen.IO Event API methods.
    /// </summary>
    internal class Event : IEvent
    {
        private readonly IProjectSettings _prjSettings;
        private readonly string _serverUrl;

        /// <summary>
        /// Get details of all schemas in the project.
        /// </summary>
        /// <returns></returns>
        public JArray GetSchemas()
        {
            try
            {
                var client = new RestClient(_serverUrl);
                var request = new RestRequest("", Method.GET);
                request.AddHeader("Authorization", _prjSettings.MasterKey);
                request.AddHeader("Keen-Sdk", KeenUtil.GetSdkVersion());

                var serverResponse = client.Execute(request);
                if (serverResponse == null)
                    throw new KeenException("No response from host");
                if (!serverResponse.ErrorMessage.IsNullOrWhiteSpace())
                    throw new KeenException("GetSchemas failed with status: " + serverResponse.ErrorMessage);

                JArray jsonResponse = null;
                try
                {
                    // The response should be an array. An error will cause a parse failure.
                    jsonResponse = JArray.Parse(serverResponse.Content);
                }
                catch (Exception)
                {
                    var obj = JObject.Parse(serverResponse.Content);
                    KeenUtil.CheckApiErrorCode(obj);
                }

                return jsonResponse;
            }
            catch (Exception ex)
            {
                throw new KeenException("GetSchemas failed", ex);
            }
        }

        /// <summary>
        /// Add all events in a single request.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public IEnumerable<CachedEvent> AddEvents(JObject events)
        {
            JObject jsonResponse = null;
            try
            {
                var client = new RestClient(_serverUrl);
                var request = new RestRequest("", Method.POST);
                request.AddHeader("Authorization", _prjSettings.WriteKey);
                request.AddHeader("Keen-Sdk", KeenUtil.GetSdkVersion());
                request.AddParameter("application/json", events.ToString(), ParameterType.RequestBody);

                var serverResponse = client.Execute(request);
                if (serverResponse == null)
                    throw new KeenException("No response from host");
                if (!serverResponse.ErrorMessage.IsNullOrWhiteSpace())
                    throw new KeenException("AddEvents failed with status: " + serverResponse.ErrorMessage);

                if (!serverResponse.Content.IsNullOrWhiteSpace())
                    jsonResponse = JObject.Parse(serverResponse.Content);
            }
            catch (Exception ex)
            {
                throw new KeenException("AddEvents failed", ex);
            }
            KeenUtil.CheckApiErrorCode(jsonResponse);

            try
            {

                // error checking, return failed events in the list,
                // or if the HTTP response is a failure, throw.
                var failedItems = from respCols in jsonResponse.Properties()
                    from eventsCols in events.Properties()
                    where respCols.Name == eventsCols.Name
                    let collection = respCols.Name
                    let combined = eventsCols.Children().Children()
                        .Zip(respCols.Children().Children(),
                            (e, r) => new {eventObj = (JObject) e, result = (JObject) r})
                    from e in combined
                    where !(bool) (e.result.Property("success").Value)
                    select new CachedEvent(collection, e.eventObj, KeenUtil.GetBulkApiError(e.result));

                return failedItems;
            }
            catch (Exception ex)
            {
                throw new KeenException("AddEvents failed", ex);
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

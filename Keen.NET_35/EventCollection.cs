using System.Diagnostics;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;

namespace Keen.NET_35
{
    /// <summary>
    /// EventCollection implements the IEventCollection interface which represents the Keen.IO EventCollection API methods.
    /// </summary>
    internal class EventCollection : IEventCollection
    {
        private readonly string _serverUrl;
        private readonly IProjectSettings _prjSettings;

        public JObject GetSchema(string collection)
        {
            try
            {
                var client = new RestClient(_serverUrl);
                var request = new RestRequest(collection, Method.GET);
                request.AddHeader("Authorization", _prjSettings.MasterKey);
                request.AddHeader("Keen-Sdk", KeenUtil.GetSdkVersion());

                var serverResponse = client.Execute(request);
                if (serverResponse==null)
                    throw new KeenException("No response from host");
                if (!serverResponse.ErrorMessage.IsNullOrWhiteSpace())
                    throw new KeenException("GetSchema failed with status: " + serverResponse.ErrorMessage);
                var response = JObject.Parse(serverResponse.Content);
                KeenUtil.CheckApiErrorCode(response);
                return response;
            }
            catch (Exception ex)
            {
                throw new KeenException("GetSchema failed", ex);
            }
        }

        public void DeleteCollection(string collection)
        {
            JObject jsonResponse = null;
            try
            {
                var client = new RestClient(_serverUrl);
                var request = new RestRequest(collection, Method.DELETE);
                request.AddHeader("Authorization", _prjSettings.MasterKey);
                request.AddHeader("Keen-Sdk", KeenUtil.GetSdkVersion());

                var serverResponse = client.Execute(request);
                if (serverResponse == null)
                    throw new KeenException("No response from host");

                if (!serverResponse.ErrorMessage.IsNullOrWhiteSpace())
                    throw new KeenException("DeleteCollection failed with error: " + serverResponse.ErrorMessage);

                if (!serverResponse.Content.IsNullOrWhiteSpace())
                    jsonResponse = JObject.Parse(serverResponse.Content);
            }
            catch (Exception ex)
            {
                throw new KeenException("DeleteCollection failed " + ex.Message, ex);
            }
            // throw an exception with information from the json response 
            KeenUtil.CheckApiErrorCode(jsonResponse);
        }

        public void AddEvent(string collection, JObject anEvent)
        {
            JObject jsonResponse = null;
            try
            {
                var client = new RestClient(_serverUrl);
                var request = new RestRequest(collection, Method.POST);
                request.AddHeader("Authorization", _prjSettings.WriteKey);
                request.AddHeader("Keen-Sdk", KeenUtil.GetSdkVersion());
                request.AddParameter("application/json", anEvent.ToString(), ParameterType.RequestBody);

                var serverResponse = client.Execute(request);
                if (serverResponse==null)
                    throw new KeenException("No response from host");
                if (!serverResponse.ErrorMessage.IsNullOrWhiteSpace())
                    throw new KeenException("AddEvent failed with status: " + serverResponse.ErrorMessage);

                if (!serverResponse.Content.IsNullOrWhiteSpace())
                    jsonResponse = JObject.Parse(serverResponse.Content);
            }
            catch (Exception ex)
            {
                throw new KeenException("AddEvent failed", ex);
            }
            KeenUtil.CheckApiErrorCode(jsonResponse);
        }

        public EventCollection(IProjectSettings prjSettings)
        {
            _prjSettings = prjSettings;

            _serverUrl = string.Format("{0}projects/{1}/{2}/",
                _prjSettings.KeenUrl, _prjSettings.ProjectId, KeenConstants.EventsResource);
        }
    }
}

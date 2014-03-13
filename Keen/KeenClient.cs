using Keen.Core.EventCache;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Keen.Core
{
    public class KeenClient
    {
        private IProjectSettings _prjSettings;
        private string keenProjectUri;

        private Dictionary<string, object> globalProperties = new Dictionary<string, object>();

        public IEventCache EventCache { get; private set; }


        /// <summary>
        /// Add a static global property. This property will be added to
        /// every event.
        /// </summary>
        /// <param name="property">Property name</param>
        /// <param name="value">Property value. This may be a simple value, array, or object,
        /// or an object that supports IDynamicPropertyValue returning one of those.</param>
        public void AddGlobalProperty(string property, object value)
        {
            // Verify that the property name is allowable, and that the value is populated.
            KeenUtil.ValidatePropertyName(property);
            if (value == null)
                throw new KeenException("Global properties must have a non-null value.");
            var dynProp = value as IDynamicPropertyValue;
            if (dynProp != null)
                // Execute the property once before it is needed to check the value
                ExecDynamicPropertyValue(property, dynProp);
            
            globalProperties.Add(property, value);
        }

        private object ExecDynamicPropertyValue(string propName, IDynamicPropertyValue dynProp)
        {
            object result;
            try
            {
                result = dynProp.Value();
            }
            catch (Exception e)
            {
                throw new KeenException(string.Format("Dynamic property \"{0}\" execution failure", propName), e);
            }
            if (result==null)
                throw new KeenException(string.Format("Dynamic property \"{0}\" execution returned null", propName));
            return result;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prjSettings">A ProjectSettings instance containing the ProjectId and API keys</param>
        public KeenClient(IProjectSettings prjSettings)
        {
            // Preconditions
            if (null == prjSettings)
                throw new KeenException("An ProjectSettings instance is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.ProjectId))
                throw new KeenException("A Project ID is required.");
            if ((string.IsNullOrWhiteSpace(prjSettings.MasterKey)
                && string.IsNullOrWhiteSpace(prjSettings.WriteKey)))
                throw new KeenException("A Master or Write API key is required.");

            _prjSettings = prjSettings;

            keenProjectUri = string.Format("{0}/{1}/projects/{2}/", 
                KeenConstants.ServerAddress, KeenConstants.ApiVersion, _prjSettings.ProjectId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prjSettings">A ProjectSettings instance containing the ProjectId and API keys</param>
        /// <param name="eventCache">An IEventCache instance providing a caching strategy</param>
        public KeenClient(IProjectSettings prjSettings, IEventCache eventCache) : this(prjSettings)
        {
            EventCache = eventCache;
        }

        /// <summary>
        /// Delete the specified collection. Deletion may be denied for collections with many events.
        /// Master API key is required.
        /// </summary>
        /// <param name="collection">Name of collection to delete.</param>
        public void DeleteCollection(string collection)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);
                var responseMsg = client.DeleteAsync(keenProjectUri + KeenConstants.EventsCollectionResource + "/" + collection).Result;
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("DeleteCollection failed with status: " + responseMsg.StatusCode);
            }
            
        }

		/// <summary>
		/// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
		/// </summary>
		/// <param name="collection"></param>
        public dynamic GetSchema(string collection)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);
                var responseMsg = client.GetAsync(keenProjectUri + KeenConstants.EventsCollectionResource + "/" + collection).Result;
                var responseString = responseMsg.Content.ReadAsStringAsync().Result;
                dynamic response = JObject.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode(response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("GetSchema failed with status: " + responseMsg.StatusCode);

                return response;
            }
        }

        /// <summary>
        /// Add a single event to the specified collection.
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventProperties">The event to add. This should be a </param>
        public void AddEvent(string collection, object eventInfo)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);
            if (null == eventInfo)
                throw new KeenException("Event data is required.");

            var jEvent = JObject.FromObject(eventInfo);
            string keenUrl = keenProjectUri + KeenConstants.EventsCollectionResource + "/" + collection;

            // Add global properties to the event
            foreach( var p in globalProperties )
            {
                // If the property value is an IDynamicPropertyValue, 
                // exec the Value() to generate the property value.
                var dynProp = p.Value as IDynamicPropertyValue;
                if (dynProp == null)
                    jEvent.Add(p.Key, JToken.FromObject(p.Value));
                else
                {
                    var val = dynProp.Value();
                    if (null == val)
                        throw new KeenException(string.Format("Dynamic property \"{0}\" returned a null value", p.Key));
                    jEvent.Add(p.Key, JToken.FromObject(val));
                }
            }

            // If an event cache has been provided, cache this event insead of sending it.
            if (null != EventCache)
                EventCache.Add(new CachedEvent(keenUrl, jEvent));
            else
            {
                HttpResponseMessage httpResponse;
                var apiResponse = KeenUtil.PostEvent(keenUrl, jEvent, _prjSettings.WriteKey, out httpResponse);

                // error checking, throw an exception with information from the 
                // json response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode(apiResponse);
                if (!httpResponse.IsSuccessStatusCode)
                    throw new KeenException("AddEvent failed with status: " + httpResponse.StatusCode);
            }
        }

        public void SendCachedEvents()
        {
            if (null==EventCache)
                throw new KeenException("Event caching is not enabled");

            foreach (var e in EventCache.Events())
            {
                // Use Event Resource API for bulk posting?
                //var keenUrl = keenProjectUri + KeenConstants.EventsCollectionResource;
                //JObject jEvent = JObject.FromObject(eventCollections);
                // Each property of eventCollections is a collection name, validate each name.
                //foreach (var i in jEvent.Properties())
                //    KeenUtil.ValidateEventCollectionName(i.Name);

                HttpResponseMessage httpResponse;
                var apiResponse = KeenUtil.PostEvent(e.Url, e.Event, _prjSettings.WriteKey, out httpResponse);

                // error checking, throw an exception with information from the 
                // json response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode(apiResponse);
                if (!httpResponse.IsSuccessStatusCode)
                    throw new KeenException("AddEvent failed with status: " + httpResponse.StatusCode);
            }
        }

    }
}

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
using System.Threading.Tasks;

namespace Keen.Core
{
    public class KeenClient
    {
        private IProjectSettings _prjSettings;
        private Dictionary<string, object> globalProperties = new Dictionary<string, object>();

        /// <summary>
        /// EventCollection provides access to the Keen.IO EventCollection API methods.
        /// The default implementation can be overridden by setting a new implementation here.
        /// </summary>
        public IEventCollection EventCollection { get; set; }

        /// <summary>
        /// EventCache provides a caching implementation allowing events to be cached locally
        /// instead of being sent one at a time. The implementation is responsible for cache 
        /// maintenance policy, such as trimming old entries to avoid excessive cache size.
        /// </summary>
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
                throw new KeenException("An IEventCollection instance is required.");
            if (null == prjSettings)
                throw new KeenException("An IProjectSettings instance is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.ProjectId))
                throw new KeenException("A Project ID is required.");
            if ((string.IsNullOrWhiteSpace(prjSettings.MasterKey)
                && string.IsNullOrWhiteSpace(prjSettings.WriteKey)))
                throw new KeenException("A Master or Write API key is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.KeenUrl))
                throw new KeenException("A URL for the server address is required.");

            _prjSettings = prjSettings;
            // The EventCollection interface normally should not need to be set by
            // callers, so the default implementation is set up here. Users may
            // override this by injecting an implementation via the property.
            EventCollection = new EventCollection(_prjSettings);
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
        public async Task DeleteCollectionAsync(string collection)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);
            if (string.IsNullOrWhiteSpace(_prjSettings.MasterKey))
                throw new KeenException("Master API key is requried for DeleteCollection");

            await EventCollection.DeleteCollection(collection);
        }

        /// <summary>
        /// Delete the specified collection. Deletion may be denied for collections with many events.
        /// Master API key is required.
        /// </summary>
        /// <param name="collection">Name of collection to delete.</param>
        public void DeleteCollection(string collection)
        {
            try
            {
                DeleteCollectionAsync(collection).Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
        /// </summary>
        /// <param name="collection"></param>
        public async Task<dynamic> GetSchemaAsync(string collection)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);
            if (string.IsNullOrWhiteSpace(_prjSettings.MasterKey))
                throw new KeenException("Master API key is requried for GetSchema");

            return await EventCollection.GetSchema(collection);
        }

		/// <summary>
		/// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
		/// </summary>
		/// <param name="collection"></param>
        public dynamic GetSchema(string collection)
        {
            try
            {
                return GetSchemaAsync(collection).Result;
            }
            catch (AggregateException ex)
            {
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Add a collection of events to the specified collection
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventsInfo">Collection of events to add</param>
        public void AddEvents(string collection, IEnumerable<object> eventsInfo)
        {
            try
            {
                AddEventsAsync(collection, eventsInfo).Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Add a collection of events to the specified collection
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventsInfo">Collection of events to add</param>
        /// <returns></returns>
        public async Task AddEventsAsync(string collection, IEnumerable<object> eventsInfo)
        {
            if (null == eventsInfo)
                throw new KeenException("AddEvents eventsInfo may not be null");

            foreach (var e in eventsInfo)
                await AddEventAsync(collection, e);
        }

        /// <summary>
        /// Add a single event to the specified collection.
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventProperties">The event to add.</param>
        public async Task AddEventAsync(string collection, object eventInfo)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);
            if (null == eventInfo)
                throw new KeenException("Event data is required.");
            if (string.IsNullOrWhiteSpace(_prjSettings.WriteKey))
                throw new KeenException("Write API key is requried for AddEvent");

            var jEvent = JObject.FromObject(eventInfo);

            // Add global properties to the event
            foreach (var p in globalProperties)
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
                EventCache.Add(new CachedEvent(collection, jEvent));
            else
                await EventCollection.AddEvent(collection, jEvent);
        }

        /// <summary>
        /// Add a single event to the specified collection.
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventProperties">The event to add. This should be a </param>
        public void AddEvent(string collection, object eventInfo)
        {
            try
            {
                AddEventAsync(collection, eventInfo).Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Submit all events found in the event cache. If an events are rejected by the server, 
        /// KeenCacheException will be thrown with a listing of the rejected events, each with
        /// the error message it received.
        /// </summary>
        public void SendCachedEvents()
        {
            try
            {
                SendCachedEventsAsync().Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Submit all events found in the event cache. If an events are rejected by the server, 
        /// KeenCacheException will be thrown with a listing of the rejected events, each with
        /// the error message it received.
        /// </summary>
        public async Task SendCachedEventsAsync()
        {
            if (null==EventCache)
                throw new KeenException("Event caching is not enabled");

            var failedEvents = new List<CachedEvent>();
            CachedEvent e;

            while( null != (e=EventCache.TryTake()))
            {
                try
                {
                    await EventCollection.AddEvent(e.Collection, e.Event);
                }
                catch (Exception ex)
                {
                    e.Error = ex;
                    failedEvents.Add(e);
                }
            }

            // if there where any failures, throw and include the errored items and details.
            if (failedEvents.Any())
                throw new KeenCacheException("One or more cached events could not be submitted", failedEvents);
        }

    }
}

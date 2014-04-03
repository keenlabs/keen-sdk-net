using Keen.Core.EventCache;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
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
        /// Event provides access to the Keen.IO Event API methods.
        /// The default implementation can be overridden by setting a enw implementation here.
        /// </summary>
        public IEvent Event { get; set; }

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
                throw new KeenException("An IProjectSettings instance is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.ProjectId))
                throw new KeenException("A Project ID is required.");
            if ((string.IsNullOrWhiteSpace(prjSettings.MasterKey)
                && string.IsNullOrWhiteSpace(prjSettings.WriteKey)))
                throw new KeenException("A Master or Write API key is required.");
            if (string.IsNullOrWhiteSpace(prjSettings.KeenUrl))
                throw new KeenException("A URL for the server address is required.");

            _prjSettings = prjSettings;
            // The EventCollection and Event interface normally should not need to 
            // be set by callers, so the default implementation is set up here. Users 
            // may override these by injecting an implementation via the property.
            EventCollection = new EventCollection(_prjSettings);
            Event = new Event(_prjSettings);

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

            await EventCollection.DeleteCollection(collection)
                .ConfigureAwait(continueOnCapturedContext: false);
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
        /// Return schema information for all the event collections in this project.
        /// </summary>
        /// <returns></returns>
        public async Task<JObject> GetSchemasAsync()
        {
            // Preconditions
            if (string.IsNullOrWhiteSpace(_prjSettings.MasterKey))
                throw new KeenException("Master API key is requried for GetSchemas");

            return await Event.GetSchemas()
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Return schema information for all the event collections in this project.
        /// </summary>
        /// <returns></returns>
        public JObject GetSchemas()
        {
            try
            {
                return Event.GetSchemas().Result;
            } catch (AggregateException ex)
            {
                throw ex.TryUnwrap();
            }
        }

        /// <summary>
        /// Retrieve the schema for the specified collection. This requires
        /// a value for the project settings Master API key.
        /// </summary>
        /// <param name="collection"></param>
        public async Task<JObject> GetSchemaAsync(string collection)
        {
            // Preconditions
            KeenUtil.ValidateEventCollectionName(collection);
            if (string.IsNullOrWhiteSpace(_prjSettings.MasterKey))
                throw new KeenException("Master API key is requried for GetSchema");

            return await EventCollection.GetSchema(collection)
                .ConfigureAwait(continueOnCapturedContext: false);
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
        /// Insert multiple events in a single request.
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
        /// Add a collection of events to the specified collection. Assumes that
        /// objects in the collection have already been through AddEvent to receive
        /// global properties. 
        /// </summary>
        /// <param name="collection">Collection name</param>
        /// <param name="eventsInfo">Collection of events to add</param>
        /// <returns>Enumerable of any rejected events</returns>
        private async Task<IEnumerable<CachedEvent>> AddEventsBulkAsync(string collection, IEnumerable<object> eventsInfo)
        {
            if (null == eventsInfo)
                throw new KeenException("AddEvents eventsInfo may not be null");
            if (!eventsInfo.Any())
                return new List<CachedEvent>();
            // Build a container object with a property to identify the collection
            var jEvent = new JObject();
            jEvent.Add(collection, JToken.FromObject(eventsInfo));

            // Use the bulk interface to add events
            return await Event.AddEvents(jEvent)
                .ConfigureAwait(continueOnCapturedContext: false);
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
            if (string.IsNullOrWhiteSpace(_prjSettings.WriteKey))
                throw new KeenException("Write API key is requried for AddEvents");

            var mainCache = EventCache;
            var localCache = new List<JObject>();

            // prepare each object to add global properties and timestamp, then either 
            // add to the main cache if it exists, or if not to the local object list.
            foreach (var e in eventsInfo)
            {
                var jEvent = PrepareUserObject(e); 
                if (null!=mainCache)
                    await mainCache.Add(new CachedEvent(collection, jEvent))
                        .ConfigureAwait(continueOnCapturedContext: false);
                else
                    localCache.Add(jEvent);
            }

            // if the local object list has data (caching is not enabled), go
            // ahead and send it using the bulk interface.
            if (localCache.Any())
            {
                var errs = await AddEventsBulkAsync(collection, localCache)
                        .ConfigureAwait(continueOnCapturedContext: false);
                if (errs.Any())
                    throw new KeenBulkException("One or more events was rejected during the bulk add operation", errs);
            }
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

            var jEvent = PrepareUserObject(eventInfo);

            // If an event cache has been provided, cache this event insead of sending it.
            if (null != EventCache)
                await EventCache.Add(new CachedEvent(collection, jEvent))
                    .ConfigureAwait(continueOnCapturedContext: false);
            else
                await EventCollection.AddEvent(collection, jEvent)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Convert a user-supplied object to a JObject that can be sent to the Keen.IO API.
        /// 
        /// This writes any global properies to the object and records the time.
        /// </summary>
        /// <param name="eventInfo"></param>
        /// <returns></returns>
        private JObject PrepareUserObject(object eventInfo)
        {
            var jEvent = JObject.FromObject(eventInfo);

            // Add global properties to the event
            foreach (var p in globalProperties)
            {
                // If the property value is an IDynamicPropertyValue, 
                // exec the Value() to generate the property value.
                var dynProp = p.Value as IDynamicPropertyValue;
                if (dynProp == null)
                {
                    KeenUtil.ValidatePropertyName(p.Key);
                    jEvent.Add(p.Key, JToken.FromObject(p.Value));
                }
                else
                {
                    var val = dynProp.Value();
                    if (null == val)
                        throw new KeenException(string.Format("Dynamic property \"{0}\" returned a null value", p.Key));
                    jEvent.Add(p.Key, JToken.FromObject(val));
                }
            }

            // Ensure this event has a 'keen' object of the correct type
            if (null == jEvent.Property("keen"))
                jEvent.Add("keen", new JObject());
            else if (jEvent.Property("keen").Value.GetType() != typeof(JObject))
                throw new KeenException(string.Format("Value of property \"keen\" must be an object, is {0}", jEvent.Property("keen").GetType()));

            // Set the keen.timestamp if it has not already been set
            JObject keen = ((JObject)jEvent.Property("keen").Value);
            if (null==keen.Property("timestamp"))
                keen.Add("timestamp", DateTime.Now);

            return jEvent;
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
                Debug.WriteLine(ex.TryUnwrap());
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

            CachedEvent e;
            var batches = new Dictionary<string, List<CachedEvent>>();
            var failedEvents = new List<CachedEvent>();

            Func<string, List<CachedEvent>> getListFor = c => 
            {
                if (batches.ContainsKey(c))
                    return batches[c];
                else
                {
                    var l = new List<CachedEvent>();
                    batches.Add(c, l);
                    return l;
                }
            };

            // Take items from the cache and sort them by collection
            while (null != (e = await EventCache.TryTake().ConfigureAwait(continueOnCapturedContext: false)))
            {
                var batch = getListFor(e.Collection);
                batch.Add(e);

                // If this collection has reached the maximum batch size, send it
                if (batch.Count == KeenConstants.BulkBatchSize)
                {
                    failedEvents.AddRange(await AddEventsBulkAsync(e.Collection, batch.Select((n) => n.Event)).ConfigureAwait(continueOnCapturedContext: false));
                    batch.Clear();
                }
            }
            
            // Send the remainder of all the collections
            foreach (var c in batches.Where(b => b.Value.Any()))
                failedEvents.AddRange(await AddEventsBulkAsync(c.Key, c.Value.Select((n) => n.Event)).ConfigureAwait(continueOnCapturedContext: false));

            // if there where any failures, throw and include the errored items and details.
            if (failedEvents.Any())
                throw new KeenBulkException("One or more cached events could not be submitted", failedEvents);
        }

    }
}

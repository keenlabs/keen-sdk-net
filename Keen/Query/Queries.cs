using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core.Query
{
    /// <summary>
    /// Queries implements the IQueries interface which represents the Keen.IO Query API methods.
    /// </summary>
    internal class Queries : IQueries
    {
        private IProjectSettings _prjSettings;
        private string _serverUrl;

        public Queries(IProjectSettings prjSettings)
        {
            _prjSettings = prjSettings;

            _serverUrl = string.Format("{0}projects/{1}/{2}",
                _prjSettings.KeenUrl, _prjSettings.ProjectId, KeenConstants.QueriesResource);
        }

        /// <summary>
        /// Add all events in a single request.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public async Task<IEnumerable<KeyValuePair<string,string>>> AvailableQueries()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);
                var responseMsg = await client.GetAsync(_serverUrl)
                    .ConfigureAwait(continueOnCapturedContext: false);
                var responseString = await responseMsg.Content.ReadAsStringAsync()
                    .ConfigureAwait(continueOnCapturedContext: false);
                var response = JObject.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode((dynamic)response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("AvailableQueries failed with status: " + responseMsg.StatusCode);

                return from j in response.Children()
                       let p = j as JProperty
                       where p != null
                       select new KeyValuePair<string, string>(p.Name, (string)p.Value);
            }
        }

        /// <summary>
        /// Returns the number of resources in the event collection.
        /// </summary>
        /// <param name="collection">Name of event collection to query</param>
        /// <param name="timeframe">Specifies window of time from which to select events for analysis</param>
        /// <param name="filters">Filter to narrow down the events used in analysis</param>
        /// <returns></returns>
        public async Task<int> Count(string collection, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null)
        {
            var result = await CountInternal(collection, filters, timeframe.ToSafeString());
            return result.Value<int>("result");
        }
        
        /// <summary>
        /// Returns a series of counts of the number of resources in the event collection.
        /// </summary>
        /// <param name="collection">Name of the event collection to query</param>
        /// <param name="timeframe">Specifies the overall window of time from which to select events for analysis</param>
        /// <param name="interval">The size of the intervals within the window</param>
        /// <param name="filters">Filters to narrow down the events used in the analysis</param>
        /// <returns></returns>
        public async Task<IEnumerable<QueryIntervalCount>> Count(string collection, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null)
        {
            if (null == interval)
                throw new ArgumentNullException("interval", "interval must be specified for a series query");
            if (null == timeframe)
                throw new ArgumentException("timeframe", "Timeframe must be specified for a series query.");

            var result = await CountInternal(collection, filters, timeframe.ToSafeString(), interval.ToSafeString());

            // project the json response into a series of 
            return from i in result.Value<JArray>("result")
                   let v = i.Value<int>("value")
                   let t = i.Value<JObject>("timeframe")
                   select new QueryIntervalCount(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
        }

        /// <summary>
        /// Internal implementation of Count
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="filters"></param>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        private async Task<JObject> CountInternal(string collection, IEnumerable<QueryFilter> filters = null, string timeframe = "", string interval = "")
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");

            Debug.WriteLine("filters: " + JArray.FromObject(filters).ToString());

            // construct the parameter list for the 'count' request
            var parms = ("?event_collection=" + collection)+
                        (filters == null ? "" : "&filters=" + Uri.EscapeDataString(JArray.FromObject(filters).ToString()))+
                        (timeframe == "" ? "" : "&timeframe=" + Uri.EscapeDataString(timeframe))+
                        (interval == "" ? "" : "&interval=" + Uri.EscapeDataString(interval));

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);

                Debug.WriteLine(_serverUrl + "/count" + parms);

                var responseMsg = await client.GetAsync(_serverUrl + "/count" + parms).ConfigureAwait(false);
                var responseString = await responseMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
                var response = JObject.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode((dynamic)response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("Count query failed with status: " + responseMsg.StatusCode);

                return response;
            }
        }
    }
}

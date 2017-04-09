using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Keen.Core.Query
{
    /// <summary>
    /// Queries implements the IQueries interface which represents the Keen.IO Query API methods.
    /// </summary>
    internal class Queries : IQueries
    {
        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _queryRelativeUrl;
        private readonly string _key;


        public Queries(IProjectSettings prjSettings,
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
            _queryRelativeUrl = KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
                                                              KeenConstants.QueriesResource);

            // TODO : The Python SDK has changed to not automatically falling back, but rather
            // throwing so that client devs learn to use the most appropriate key. So here we
            // really could or should just demand the ReadKey.
            _key = string.IsNullOrWhiteSpace(prjSettings.MasterKey) ?
                prjSettings.ReadKey : prjSettings.MasterKey;
        }

        public Queries(IProjectSettings prjSettings)
            : this(prjSettings, new KeenHttpClientProvider())
        {
        }


        private async Task<JObject> KeenWebApiRequest(string operation = "",
                                                      Dictionary<string, string> parms = null)
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                throw new KeenException("An API ReadKey or MasterKey is required.");
            }

            var parmVals = (parms == null) ?
                "" : string.Join("&", from p in parms.Keys
                                      where !string.IsNullOrEmpty(parms[p])
                                      select string.Format("{0}={1}",
                                                           p,
                                                           Uri.EscapeDataString(parms[p])));
            var url = string.Format("{0}{1}{2}",
                                       _queryRelativeUrl,
                                       string.IsNullOrWhiteSpace(operation) ? "" : "/" + operation,
                                       string.IsNullOrWhiteSpace(parmVals) ? "" : "?" + parmVals);
            var responseMsg = await _keenHttpClient.GetAsync(url, _key).ConfigureAwait(false);
            var responseString = await responseMsg
                                    .Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);
            var response = JObject.Parse(responseString);

            // error checking, throw an exception with information from the json
            // response if available, then check the HTTP response.
            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException("Request failed with status: " +
                                        responseMsg.StatusCode);
            }

            return response;
        }

        public async Task<IEnumerable<KeyValuePair<string,string>>> AvailableQueries()
        {
            var reply = await KeenWebApiRequest().ConfigureAwait(false);
            return from j in reply.Children()
                   let p = j as JProperty
                   where p != null 
                   select new KeyValuePair<string, string>(p.Name, (string)p.Value);
        }

        #region metric

        public async Task<JObject> Metric(string queryName, Dictionary<string,string> parms)
        {
            if (string.IsNullOrEmpty(queryName))
                throw new ArgumentNullException("queryName");
            if (null==parms)
                throw new ArgumentNullException("parms");

            return await KeenWebApiRequest(queryName, parms).ConfigureAwait(false);
        }

        public async Task<string> Metric(QueryType queryType, string collection, string targetProperty, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty) && (queryType != QueryType.Count()))
                throw new ArgumentNullException("targetProperty");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(queryType.ToString(), parms).ConfigureAwait(false);

            string result;
            if (queryType == QueryType.SelectUnique())
                // This is to support SelectUnique which is the only query type with a list-type result.
                result = string.Join(",", (reply.Value<JArray>("result").Values<string>()));
            else
                result = reply.Value<string>("result");
            return result;
        }

        public async Task<IEnumerable<QueryGroupValue<string>>> Metric(QueryType queryType, string collection, string targetProperty, string groupby, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty) && (queryType != QueryType.Count()))
                throw new ArgumentNullException("targetProperty");
            if (string.IsNullOrWhiteSpace(groupby))
                throw new ArgumentNullException("groupby", "groupby field name must be specified for a groupby query");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty);
            parms.Add(KeenConstants.QueryParmGroupBy, groupby);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(queryType.ToString(), parms).ConfigureAwait(false);

            IEnumerable<QueryGroupValue<string>> result;
            if (queryType == QueryType.SelectUnique())
            {
                // This is to support SelectUnique which is the only query type with a list-type result.
                result = from r in reply.Value<JArray>("result")
                         let c = string.Join(",", r.Value<JArray>("result").Values<string>())
                         let g = r.Value<string>(groupby)
                         select new QueryGroupValue<string>(c, g);
            }
            else
            {
                result = from r in reply.Value<JArray>("result")
                         let c = r.Value<string>("result")
                         let g = r.Value<string>(groupby)
                         select new QueryGroupValue<string>(c, g);
            }
            return result;
        }

        public async Task<IEnumerable<QueryIntervalValue<string>>> Metric(QueryType queryType, string collection, string targetProperty, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty) && (queryType != QueryType.Count()))
                throw new ArgumentNullException("targetProperty");
            if (null == timeframe)
                throw new ArgumentException("timeframe", "Timeframe must be specified for a series query.");
            if (null == interval)
                throw new ArgumentNullException("interval", "interval must be specified for a series query");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmInterval, interval.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(queryType.ToString(), parms).ConfigureAwait(false);

            IEnumerable<QueryIntervalValue<string>> result;
            if (queryType == QueryType.SelectUnique())
            {
                // This is to support SelectUnique which is the only query type with a list-type result.
                result = from i in reply.Value<JArray>("result")
                         let v = string.Join(",", i.Value<JArray>("value").Values<string>())
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<string>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }
            else
            {
                result = from i in reply.Value<JArray>("result")
                         let v = i.Value<string>("value")
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<string>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }

            return result;
        }

        public async Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>>> Metric(QueryType queryType, string collection, string targetProperty, string groupby, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (queryType == null)
                throw new ArgumentNullException("queryType");
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty) && (queryType != QueryType.Count()))
                throw new ArgumentNullException("targetProperty");
            if (null == timeframe)
                throw new ArgumentException("timeframe", "Timeframe must be specified for a series query.");
            if (null == interval)
                throw new ArgumentNullException("interval", "interval must be specified for a series query");
            if (string.IsNullOrWhiteSpace(groupby))
                throw new ArgumentNullException("groupby", "groupby field name must be specified for a goupby query");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty);
            parms.Add(KeenConstants.QueryParmGroupBy, groupby);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmInterval, interval.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(queryType.ToString(), parms).ConfigureAwait(false);

            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>> result;
            if (queryType == QueryType.SelectUnique())
            {
                // This is to support SelectUnique which is the only query type with a list-type result.
                result = from i in reply.Value<JArray>("result")
                         let v = (from r in i.Value<JArray>("value")
                                  let c = string.Join(",", r.Value<JArray>("result").Values<string>())
                                  let g = r.Value<string>(groupby)
                                  select new QueryGroupValue<string>(c, g))
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }
            else
            {
                result = from i in reply.Value<JArray>("result")
                         let v = (from r in i.Value<JArray>("value")
                                  let c = r.Value<string>("result")
                                  let g = r.Value<string>(groupby)
                                  select new QueryGroupValue<string>(c, g))
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }
            return result;
        }

        #endregion metric

        public async Task<IEnumerable<dynamic>> Extract(string collection, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, int latest = 0, string email = "")
        {
            var parms = new Dictionary<string, string>();
             parms.Add(KeenConstants.QueryParmEventCollection, collection);
             parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
             parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());
             parms.Add(KeenConstants.QueryParmEmail, email);
             parms.Add(KeenConstants.QueryParmLatest, latest > 0 ? latest.ToString() : "");

            var reply = await KeenWebApiRequest(KeenConstants.QueryExtraction, parms).ConfigureAwait(false);

            return from i in reply.Value<JArray>("result") select (dynamic)i;
        }

        public async Task<FunnelResult> Funnel(IEnumerable<FunnelStep> steps,
            QueryTimeframe timeframe = null, string timezone = "")
        {
            var jObs = steps.Select(i => JObject.FromObject(i));
            var stepsJson = new JArray(jObs).ToString();

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmSteps, stepsJson);

            var reply = await KeenWebApiRequest(KeenConstants.QueryFunnel, parms).ConfigureAwait(false);
            var o = reply.ToObject<FunnelResult>();
            return o;
        }



        public async Task<IDictionary<string,string>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            var jObs = analysisParams.Select(x => 
                new JProperty( x.Label, JObject.FromObject( new {analysis_type = x.Analysis, target_property = x.TargetProperty })));
            var parmsJson = JsonConvert.SerializeObject(new JObject(jObs), Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());
            parms.Add(KeenConstants.QueryParmAnalyses, parmsJson);

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms).ConfigureAwait(false);

            var result = new Dictionary<string, string>();
            foreach (JProperty i in reply.Value<JObject>("result").Children())
                result.Add(i.Name, (string)i.Value);

            return result;
        }

        public async Task<IEnumerable<QueryGroupValue<IDictionary<string, string>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string groupby = "", string timezone = "")
        {
            var jObs = analysisParams.Select(x =>
                new JProperty(x.Label, JObject.FromObject(new { analysis_type = x.Analysis, target_property = x.TargetProperty })));
            var parmsJson = JsonConvert.SerializeObject(new JObject(jObs), Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());
            parms.Add(KeenConstants.QueryParmGroupBy, groupby);
            parms.Add(KeenConstants.QueryParmAnalyses, parmsJson);

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms).ConfigureAwait(false);

            var result = new List<QueryGroupValue<IDictionary<string,string>>>();
            foreach (JObject i in reply.Value<JArray>("result"))
            {
                var d = new Dictionary<string, string>();
                string grpVal = "";
                foreach (JProperty p in i.Values<JProperty>())
                {
                    if (p.Name == groupby)
                        grpVal = (string)p.Value;
                    else
                        d.Add(p.Name, (string)p.Value);
                }
                var qg = new QueryGroupValue<IDictionary<string, string>>(d, grpVal);
                result.Add(qg);
            }

            return result;
        }

        public async Task<IEnumerable<QueryIntervalValue<IDictionary<string,string>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, QueryInterval interval = null, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            var jObs = analysisParams.Select(x => new JProperty(x.Label, JObject.FromObject(new { analysis_type = x.Analysis, target_property = x.TargetProperty })));
            var parmsJson = JsonConvert.SerializeObject(new JObject(jObs), Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmInterval, interval.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());
            parms.Add(KeenConstants.QueryParmAnalyses, parmsJson);

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms).ConfigureAwait(false);

            var result = new List<QueryIntervalValue<IDictionary<string, string>>>();
            foreach (JObject i in reply.Value<JArray>("result"))
            {
                var d = new Dictionary<string, string>();
                foreach (JProperty p in i.Value<JObject>("value").Values<JProperty>())
                    d.Add(p.Name, (string)p.Value);

                var t = i.Value<JObject>("timeframe");
                var qv = new QueryIntervalValue<IDictionary<string, string>>(d , t.Value<DateTime>("start"), t.Value<DateTime>("end"));
                result.Add(qv);
            }

            return result;
        }

        public async Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, QueryInterval interval = null, IEnumerable<QueryFilter> filters = null, string groupby = "", string timezone = "")
        {
            var jObs = analysisParams.Select(x => new JProperty(x.Label, JObject.FromObject(new { analysis_type = x.Analysis, target_property = x.TargetProperty })));
            var parmsJson = JsonConvert.SerializeObject(new JObject(jObs), Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmInterval, interval.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmGroupBy, groupby);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());
            parms.Add(KeenConstants.QueryParmAnalyses, parmsJson);

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms).ConfigureAwait(false);

            var result = new List<QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>>();
            foreach (JObject i in reply.Value<JArray>("result"))
            {
                var qgl = new List<QueryGroupValue<IDictionary<string, string>>>();
                foreach (JObject o in i.Value<JArray>("value"))
                {
                    var d = new Dictionary<string, string>();
                    string grpVal = "";
                    foreach (JProperty p in o.Values<JProperty>())
                    {
                        if (p.Name == groupby)
                            grpVal = (string)p.Value;
                        else
                            d.Add(p.Name, (string)p.Value);
                    }
                    qgl.Add( new QueryGroupValue<IDictionary<string, string>>(d, grpVal));
                }

                var t = i.Value<JObject>("timeframe");
                var qv = new QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>(qgl, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
                result.Add(qv);
            }
            return result;
        }
    }
}

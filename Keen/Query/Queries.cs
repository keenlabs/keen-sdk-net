using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
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


        private async Task<JObject> KeenWebApiRequest(string operation = "", Dictionary<string, string> parms = null)
        {
            var parmVals = parms == null ? "" : string.Join("&", from p in parms.Keys
                                                                 where !string.IsNullOrEmpty(parms[p])
                                                                 select string.Format("{0}={1}", p, Uri.EscapeDataString(parms[p])));

            var url = string.Format("{0}{1}{2}",
                _serverUrl,
                string.IsNullOrWhiteSpace(operation) ? "" : "/" + operation,
                string.IsNullOrWhiteSpace(parmVals) ? "" : "?" + parmVals);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", _prjSettings.MasterKey);

                var responseMsg = await client.GetAsync(url).ConfigureAwait(false);
                var responseString = await responseMsg.Content.ReadAsStringAsync().ConfigureAwait(false);
                var response = JObject.Parse(responseString);

                // error checking, throw an exception with information from the json 
                // response if available, then check the HTTP response.
                KeenUtil.CheckApiErrorCode((dynamic)response);
                if (!responseMsg.IsSuccessStatusCode)
                    throw new KeenException("Request failed with status: " + responseMsg.StatusCode);

                return response;
            }
        }

        public async Task<IEnumerable<KeyValuePair<string,string>>> AvailableQueries()
        {
            var reply = await KeenWebApiRequest();
            return from j in reply.Children()
                   let p = j as JProperty
                   where p != null
                   select new KeyValuePair<string, string>(p.Name, (string)p.Value);
        }

        #region metric
        public async Task<T> Metric<T>(string metric, string collection, string targetProperty, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty))
                throw new ArgumentNullException("targetProperty");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty == "-" ? "" : targetProperty);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(metric, parms);

            T result;
            if ((reply.GetValue("result") is JArray) &&  (typeof(T)==(typeof(IEnumerable<string>))))
                // This is specifically to support SelectUnique which will call with T as IEnumerable<string>
                result = (T)(IEnumerable<string>)(reply.Value<JArray>("result").Values<string>());
            else
                result = reply.Value<T>("result");
            return result;
        }

        public async Task<IEnumerable<QueryGroupValue<T>>> Metric<T>(string metric, string collection, string targetProperty, string groupby, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty))
                throw new ArgumentNullException("targetProperty");
            if (string.IsNullOrWhiteSpace(groupby))
                throw new ArgumentNullException("groupby", "groupby field name must be specified for a goupby query");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty == "-" ? "" : targetProperty);
            parms.Add(KeenConstants.QueryParmGroupBy, groupby);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(metric, parms);

            IEnumerable<QueryGroupValue<T>> result;
            if ((reply.GetValue("result") is JArray)
            && (typeof(T) == (typeof(IEnumerable<string>))))
            {
                // This is specifically to support SelectUnique which will call with T as IEnumerable<string>
                result = from r in reply.Value<JArray>("result")
                         let c = (T)r.Value<JArray>("result").Values<string>()
                         let g = r.Value<string>(groupby)
                         select new QueryGroupValue<T>(c, g);
            }
            else
            {
                result = from r in reply.Value<JArray>("result")
                         let c = (T)r.Value<T>("result")
                         let g = r.Value<string>(groupby)
                         select new QueryGroupValue<T>(c, g);
            }
            return result;
        }

        public async Task<IEnumerable<QueryIntervalValue<T>>> Metric<T>(string metric, string collection, string targetProperty, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty))
                throw new ArgumentNullException("targetProperty");
            if (null == timeframe)
                throw new ArgumentException("timeframe", "Timeframe must be specified for a series query.");
            if (null == interval)
                throw new ArgumentNullException("interval", "interval must be specified for a series query");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty == "-" ? "" : targetProperty);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmInterval, interval.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(metric, parms);

            IEnumerable<QueryIntervalValue<T>> result;
            if ((reply.GetValue("result") is JArray)
            && (typeof(T) == (typeof(IEnumerable<string>))))
            {
                // This is specifically to support SelectUnique which will call with T as IEnumerable<string>
                result = from i in reply.Value<JArray>("result")
                         let v = (T)i.Value<JArray>("value").Values<string>()
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<T>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }
            else
            {
                result = from i in reply.Value<JArray>("result")
                         let v = i.Value<T>("value")
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<T>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }

            return result;
        }

        public async Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<T>>>>> Metric<T>(string metric, string collection, string targetProperty, string groupby, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "")
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException("collection");
            if (string.IsNullOrWhiteSpace(targetProperty))
                throw new ArgumentNullException("targetProperty");
            if (null == timeframe)
                throw new ArgumentException("timeframe", "Timeframe must be specified for a series query.");
            if (null == interval)
                throw new ArgumentNullException("interval", "interval must be specified for a series query");
            if (string.IsNullOrWhiteSpace(groupby))
                throw new ArgumentNullException("groupby", "groupby field name must be specified for a goupby query");

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmEventCollection, collection);
            parms.Add(KeenConstants.QueryParmTargetProperty, targetProperty == "-" ? "" : targetProperty);
            parms.Add(KeenConstants.QueryParmGroupBy, groupby);
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmInterval, interval.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmFilters, filters == null ? "" : JArray.FromObject(filters).ToString());

            var reply = await KeenWebApiRequest(metric, parms);

            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<T>>>> result;
            if ((reply.GetValue("result") is JArray)
            && (typeof(T) == (typeof(IEnumerable<string>))))
            {
                // This is specifically to support SelectUnique which will call with T as IEnumerable<string>
                result = from i in reply.Value<JArray>("result")
                         let v = (from r in i.Value<JArray>("value")
                                  let c = (T)r.Value<JArray>("result").Values<string>()
                                  let g = r.Value<string>(groupby)
                                  select new QueryGroupValue<T>(c, g))
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<IEnumerable<QueryGroupValue<T>>>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
            }
            else
            {
                result = from i in reply.Value<JArray>("result")
                         let v = (from r in i.Value<JArray>("value")
                                  let c = (T)r.Value<T>("result")
                                  let g = r.Value<string>(groupby)
                                  select new QueryGroupValue<T>(c, g))
                         let t = i.Value<JObject>("timeframe")
                         select new QueryIntervalValue<IEnumerable<QueryGroupValue<T>>>(v, t.Value<DateTime>("start"), t.Value<DateTime>("end"));
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

            var reply = await KeenWebApiRequest(KeenConstants.QueryExtraction, parms);

            return from i in reply.Value<JArray>("result") select (dynamic)i;
        }



        public async Task<IEnumerable<int>> Funnel(string collection, IEnumerable<FunnelStep> steps, QueryTimeframe timeframe = null, string timezone = "")
        {
            var jObs = steps.Select(i=>JObject.FromObject(i));
            var stepsJson = new JArray( jObs ).ToString();

            var parms = new Dictionary<string, string>();
            parms.Add(KeenConstants.QueryParmTimeframe, timeframe.ToSafeString());
            parms.Add(KeenConstants.QueryParmTimezone, timezone);
            parms.Add(KeenConstants.QueryParmSteps, stepsJson);

            var reply = await KeenWebApiRequest(KeenConstants.QueryFunnel, parms);

            return from i in reply.Value<JArray>("result") select (int)i;
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

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms);

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

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms);

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

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms);

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

            var reply = await KeenWebApiRequest(KeenConstants.QueryMultiAnalysis, parms);

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

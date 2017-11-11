using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace Keen.Query
{
    public interface IQueries
    {
        /// <summary>
        /// Returns a list of available queries and links to them.
        /// </summary>
        Task<IEnumerable<KeyValuePair<string, string>>> AvailableQueries();

        Task<JObject> Metric(string queryName, Dictionary<string, string> parms);

        Task<string> Metric(QueryType queryType, string collection, string targetProperty, IQueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "");

        Task<IEnumerable<QueryGroupValue<string>>> Metric(QueryType queryType, string collection, string targetProperty, string groupBy, IQueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryIntervalValue<string>>> Metric(QueryType queryType, string collection, string targetProperty, IQueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>>> Metric(QueryType queryType, string collection, string targetProperty, string groupBy, IQueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "");

        Task<IDictionary<string, string>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, IQueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryGroupValue<IDictionary<string, string>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, IQueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string groupby = "", string timezone = "");
        Task<IEnumerable<QueryIntervalValue<IDictionary<string, string>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, IQueryTimeframe timeframe = null, QueryInterval interval = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, IQueryTimeframe timeframe = null, QueryInterval interval = null, IEnumerable<QueryFilter> filters = null, string groupby = "", string timezone = "");

        Task<IEnumerable<dynamic>> Extract(string collection, IQueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, int latest = 0, string email = "");

        Task<FunnelResult> Funnel(IEnumerable<FunnelStep> steps, IQueryTimeframe timeframe = null, string timeZone = "");
    }
}

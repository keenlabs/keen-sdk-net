using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core.Query
{
    public interface IQueries
    {
        /// <summary>
        /// Returns a list of available queries and links to them.
        /// </summary>
        Task<IEnumerable<KeyValuePair<string, string>>> AvailableQueries();

        Task<T> Metric<T>(string metric, string collection, string targetProperty, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryGroupValue<T>>> Metric<T>(string metric, string collection, string targetProperty, string groupBy, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryIntervalValue<T>>> Metric<T>(string metric, string collection, string targetProperty, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<T>>>>> Metric<T>(string metric, string collection, string targetProperty, string groupBy, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null, string timezone = "");

        Task<IDictionary<string, string>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryGroupValue<IDictionary<string, string>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, string groupby = "", string timezone = "");
        Task<IEnumerable<QueryIntervalValue<IDictionary<string, string>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, QueryInterval interval = null, IEnumerable<QueryFilter> filters = null, string timezone = "");
        Task<IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>>> MultiAnalysis(string collection, IEnumerable<MultiAnalysisParam> analysisParams, QueryTimeframe timeframe = null, QueryInterval interval = null, IEnumerable<QueryFilter> filters = null, string groupby = "", string timezone = "");

        Task<IEnumerable<dynamic>> Extract(string collection, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null, int latest = 0, string email = "");

        Task<IEnumerable<int>> Funnel(string collection, IEnumerable<FunnelStep> steps, QueryTimeframe timeframe = null, string timeZone = "" );

    }
}

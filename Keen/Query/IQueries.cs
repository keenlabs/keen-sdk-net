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

        Task<int> Count(string collection, QueryTimeframe timeframe = null, IEnumerable<QueryFilter> filters = null);
        Task<IEnumerable<QueryIntervalCount>> Count(string collection, QueryTimeframe timeframe, QueryInterval interval, IEnumerable<QueryFilter> filters = null);

        //select_unique_url
        //minimum
        //extraction_url
        //percentile
        //funnel_url
        //average
        //median
        //maximum

        //count_unique_url
        //sum
    }
}

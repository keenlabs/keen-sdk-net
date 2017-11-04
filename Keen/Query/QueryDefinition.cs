using System.Collections.Generic;


namespace Keen.Core.Query
{
    /// <summary>
    /// Holds information describing the query that is cached within a cached dataset.
    /// </summary>
    public class QueryDefinition
    {
        /// <summary>
        /// Unique id of the project to analyze.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// The type of analysis for this query (e.g. count, count_unique, sum etc.)
        /// </summary>
        public string AnalysisType { get; set; }

        /// <summary>
        /// Specifies the name of the event collection to analyze.
        /// </summary>
        public string EventCollection { get; set; }

        /// <summary>
        /// Refines the scope of events to be included in the analysis based on event property
        /// values.
        /// </summary>
        public IEnumerable<QueryFilter> Filters { get; set; }

        /// <summary>
        /// Limits analysis to a specific period of time when the events occurred.
        /// </summary>
        public string Timeframe { get; set; }

        /// <summary>
        /// Assigns a timezone offset to relative timeframes.
        /// </summary>
        public string Timezone { get; set; }

        /// <summary>
        /// Specifies the size of time interval by which to group results. Using this parameter
        /// changes the response format.
        /// </summary>
        public string Interval { get; set; }

        /// <summary>
        /// Specifies the names of properties by which to group results. Using this parameter
        /// changes the response format.
        /// </summary>
        public IEnumerable<string> GroupBy { get; set; }
    }

    
}

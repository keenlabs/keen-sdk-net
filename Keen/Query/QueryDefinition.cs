namespace Keen.Core.Query
{
    using System.Collections.Generic;

    public class QueryDefinition
    {
        public string ProjectId { get; set; }
        public string AnalysisType { get; set; }
        public string EventCollection { get; set; }
        public IEnumerable<QueryFilter> Filters { get; set; }
        public string Timeframe { get; set; }
        public string Timezone { get; set; }
        public string Interval { get; set; }
        public IEnumerable<string> GroupBy { get; set; }
    }
}

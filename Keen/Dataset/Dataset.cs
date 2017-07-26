using System;
using System.Collections.Generic;

namespace Keen.Core.Dataset
{
    using Query;

    public class DatasetDefinition
    {
        public string DatasetName { get; set; }
        public string DisplayName { get; set; }
        public QueryDefinition Query { get; set; }
        public IEnumerable<string> IndexBy { get; set; }
        public DateTime LastScheduledDate { get; set; }
        public DateTime LatestSubtimeframeAvailable { get; set; }
        public long MillisecondsBehind { get; set; }
    }
}

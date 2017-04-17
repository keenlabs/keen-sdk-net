using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;


namespace Keen.Core.Query
{
    /// <summary>
    /// A pair of dates representing a time interval.
    /// </summary>
    public sealed class QueryAbsoluteTimeframe : IQueryTimeframe
    {
        [JsonProperty(PropertyName = "start")]
        public DateTime Start { get; private set; }

        [JsonProperty(PropertyName = "end")]
        public DateTime End { get; private set; }

        public override string ToString()
        {
            return JObject.FromObject(this).ToString(Formatting.None);
        }

        public QueryAbsoluteTimeframe(DateTime start, DateTime end)
        {
            if (start >= end)
                throw new ArgumentException("Start date must be before stop date.");

            Start = start;
            End = end;
        }
    }
}

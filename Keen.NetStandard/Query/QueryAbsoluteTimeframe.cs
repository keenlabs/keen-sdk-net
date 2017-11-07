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

        public override bool Equals(object obj)
        {
            var timeframe = obj as QueryAbsoluteTimeframe;
            return timeframe != null &&
                   Start == timeframe.Start &&
                   End == timeframe.End;
        }

        public override int GetHashCode()
        {
            var hashCode = -1676728671;
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + End.GetHashCode();
            return hashCode;
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

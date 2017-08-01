using Newtonsoft.Json;
using System.Collections.Generic;


namespace Keen.NetStandard.Query
{
    public class FunnelResultStep
    {
        [JsonProperty(PropertyName = "with_actors")]
        public bool WithActors { get; set; }

        [JsonProperty(PropertyName = "actor_property")]
        public string ActorProperty { get; set; }

        [JsonProperty(PropertyName = "filters")]
        public IEnumerable<QueryFilter> Filters { get; set; }

        [JsonProperty(PropertyName = "timeframe")]
        [JsonConverter(typeof(TimeframeConverter))]
        public IQueryTimeframe Timeframe { get; set; }

        [JsonProperty(PropertyName = "timezone")]
        public string TimeZone { get; set; }

        [JsonProperty(PropertyName = "event_collection")]
        public string EventCollection { get; set; }

        [JsonProperty(PropertyName = "optional")]
        public bool Optional { get; set; }

        [JsonProperty(PropertyName = "inverted")]
        public bool Inverted { get; set; }
    }
}

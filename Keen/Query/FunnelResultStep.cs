using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Keen.Core.Query
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
        public QueryTimeframe Timeframe { get; set; }

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

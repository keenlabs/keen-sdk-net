using System.Collections.Generic;
using Newtonsoft.Json;


namespace Keen.Core.Query
{
    /// <summary>
    /// Represents one step in a funnel query. See the Keen.IO API for details on how to perform a funnel query.
    /// </summary>
    public class FunnelStep
    {
        /// <summary>
        ///  The name of the event that defines the step.
        /// </summary>
        [JsonProperty(PropertyName = "event_collection")]
        public string EventCollection { get; set; }

        /// <summary>
        /// The name of the property that can be used as a unique identifier for a user (or any type of actor).
        /// </summary>
        [JsonProperty(PropertyName = "actor_property")]
        public string ActorProperty { get; set; }

        /// <summary>
        /// Filters are used to narrow the scope of events used in this step of the funnel.
        /// </summary>
        [JsonProperty(PropertyName = "filters", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<QueryFilter> Filters { get; set; }

        /// <summary>
        /// If set to true, events matching this step will be excluded from the funnel.
        /// May not be applied to an initial step.
        /// </summary>
        [JsonProperty(PropertyName = "inverted", NullValueHandling = NullValueHandling.Ignore)]
        public bool Inverted { get; set; }

        /// <summary>
        /// If set to true, filtering applied to this step won't apply to any steps after it.
        /// May not be applied to an initial step.
        /// </summary>
        [JsonProperty(PropertyName = "optional", NullValueHandling = NullValueHandling.Ignore)]
        public bool Optional { get; set; }

        /// <summary>
        /// Window of time to use for the analysis. If not set, the timeframe from the funnel will be inherited, if available.
        /// </summary>
        [JsonProperty(PropertyName = "timeframe", NullValueHandling = NullValueHandling.Ignore)]
        public IQueryTimeframe Timeframe { get; set; }

        /// <summary>
        /// Offset from UTC in seconds. If not set, the timezone from the funnel will be inherited, if available.
        /// </summary>
        [JsonProperty(PropertyName = "timezone", NullValueHandling = NullValueHandling.Ignore)]
        public string TimeZone { get; set; }

        /// <summary>
        /// If set to true, a list of unique actor_properties will be returned for each step as the 'actors' 
        /// attribute alongside the 'results' attribute.
        /// </summary>
        [JsonProperty(PropertyName = "with_actors", NullValueHandling = NullValueHandling.Ignore)]
        public bool WithActors { get; set; }
    }
}

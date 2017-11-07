using Newtonsoft.Json;
using System.Collections.Generic;


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

        public override bool Equals(object obj)
        {
            var step = obj as FunnelResultStep;
            return step != null &&
                   WithActors == step.WithActors &&
                   ActorProperty == step.ActorProperty &&
                   EqualityComparer<IEnumerable<QueryFilter>>.Default.Equals(Filters, step.Filters) &&
                   EqualityComparer<IQueryTimeframe>.Default.Equals(Timeframe, step.Timeframe) &&
                   TimeZone == step.TimeZone &&
                   EventCollection == step.EventCollection &&
                   Optional == step.Optional &&
                   Inverted == step.Inverted;
        }

        public override int GetHashCode()
        {
            var hashCode = 1007130157;
            hashCode = hashCode * -1521134295 + WithActors.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ActorProperty);
            hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<QueryFilter>>.Default.GetHashCode(Filters);
            hashCode = hashCode * -1521134295 + EqualityComparer<IQueryTimeframe>.Default.GetHashCode(Timeframe);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TimeZone);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EventCollection);
            hashCode = hashCode * -1521134295 + Optional.GetHashCode();
            hashCode = hashCode * -1521134295 + Inverted.GetHashCode();
            return hashCode;
        }
    }
}

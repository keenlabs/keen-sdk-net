using Newtonsoft.Json;
using System.Collections.Generic;


namespace Keen.Core.Query
{
    public class FunnelResult
    {
        [JsonProperty(PropertyName = "actors", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<IEnumerable<dynamic>> Actors { get; set; }

        [JsonProperty(PropertyName = "steps", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<FunnelResultStep> Steps { get; set; }

        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<int> Result{ get; set; }
    }
}

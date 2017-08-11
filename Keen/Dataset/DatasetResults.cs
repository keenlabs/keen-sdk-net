using Newtonsoft.Json.Linq;

namespace Keen.Core.Dataset
{
    internal class DatasetResults
    {
        public JToken Result { get; set; }
        public DatasetMetadata Metadata { get; set; }
    }
}

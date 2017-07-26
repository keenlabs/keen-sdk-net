namespace Keen.Core.Dataset
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class DatasetResults
    {
        public JToken Result { get; set; }
        public DatasetMetadata Metadata { get; set; }
    }
}

namespace Keen.Core.Dataset
{
    using Newtonsoft.Json;

    public class DatasetMetadata
    {
        public DatasetDefinition Dataset { get; set; }
        public string Request { get; set; }
    }
}

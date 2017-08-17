using System.Collections.Generic;

namespace Keen.Core.Dataset
{
    public class DatasetDefinitionCollection
    {
        public IEnumerable<DatasetDefinition> Datasets { get; set; }
        public string NextPageUrl { get; set; }
        public int Count { get; set; }
    }
}

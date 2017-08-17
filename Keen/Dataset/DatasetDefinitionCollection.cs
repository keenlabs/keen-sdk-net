using System.Collections.Generic;

namespace Keen.Core.Dataset
{
    public class DatasetDefinitionCollection
    {
        /// <summary>
        /// List of all the DatasetDefinitions returns in this page
        /// </summary>
        public IEnumerable<DatasetDefinition> Datasets { get; set; }
        /// <summary>
        /// The url of the next page of DatasetDefinitions
        /// </summary>
        public string NextPageUrl { get; set; }
        /// <summary>
        /// The total amount of Cached Datasets in the project
        /// </summary>
        public int Count { get; set; }
    }
}

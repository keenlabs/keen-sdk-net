using System.Collections.Generic;


namespace Keen.Dataset
{
    /// <summary>
    /// A model for a collection of DatasetDefinitions with paging information.
    /// </summary>
    public class DatasetDefinitionCollection
    {
        /// <summary>
        /// A list of the DatasetDefinitions returned in this page.
        /// </summary>
        public IEnumerable<DatasetDefinition> Datasets { get; set; }

        /// <summary>
        /// The url of the next page of Dataset definitions.
        /// </summary>
        public string NextPageUrl { get; set; }

        /// <summary>
        /// The total count of Cached Datasets for this project.
        /// </summary>
        public int Count { get; set; }
    }
}

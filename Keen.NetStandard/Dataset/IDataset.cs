using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace Keen.Core.Dataset
{
    public interface IDataset
    {
        /// <summary>
        /// Get query results from a Cached Dataset.
        /// </summary>
        /// <param name="datasetName">Name of cached dataset to query.</param>
        /// <param name="indexBy">The string property value by which to retrieve results.</param>
        /// <param name="timeframe">Limits retrieval of results to a specific portion of the
        ///   Cached Dataset</param>
        /// <returns>A JObject containing query results and metadata defining the cached
        ///   dataset.</returns>
        Task<JObject> GetResultsAsync(string datasetName, string indexBy, string timeframe);

        /// <summary>
        /// Get the definition of your cached dataset.
        /// </summary>
        /// <param name="datasetName">Name of cached dataset for which to retrieve the
        ///   definition.</param>
        /// <returns>An DatasetDefinition containing metadata about a cached dataset.</returns>
        Task<DatasetDefinition> GetDefinitionAsync(string datasetName);

        /// <summary>
        /// Lists the first n cached dataset definitions in your project.
        /// </summary>
        /// <param name="limit">How many cached dataset definitions to return at a time (1-100).
        ///   Defaults to 10.</param>
        /// <param name="afterName">A cursor for use in pagination. afterName is the Cached Dataset
        ///   name that defines your place in the list.</param>
        Task<DatasetDefinitionCollection> ListDefinitionsAsync(int limit = 10,
                                                               string afterName = null);

        /// <summary>
        /// Lists all the dataset definitions in the project.
        /// </summary>
        /// <returns>An enumerable of DatasetDefinitions.</returns>
        Task<IEnumerable<DatasetDefinition>> ListAllDefinitionsAsync();

        /// <summary>
        /// Delete a Cached Dataset 
        /// </summary>
        /// <param name="datasetName">The name of the dataset to be deleted.</param>
        Task DeleteDatasetAsync(string datasetName);

        /// <summary>
        /// Creates a new Cached Dataset
        /// </summary>
        /// <param name="dataset">An instance of DatasetDefinition. At minimum, it must have
        ///   DatasetName, DisplayName, IndexBy and Query populated.</param>
        /// <returns>An instance of DatasetDefinition populated with more information about the
        ///   create Dataset.</returns>
        Task<DatasetDefinition> CreateDatasetAsync(DatasetDefinition dataset);
    }
}

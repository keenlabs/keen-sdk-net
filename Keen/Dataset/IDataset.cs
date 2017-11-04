using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Keen.Core.Dataset
{
    public interface IDataset
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="indexBy"></param>
        /// <param name="timeframe"></param>
        /// <returns></returns>
        Task<JObject> GetResultsAsync(string datasetName, string indexBy, string timeframe);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datasetName"></param>
        /// <returns></returns>
        Task<DatasetDefinition> GetDefinitionAsync(string datasetName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="afterName"></param>
        /// <returns></returns>
        Task<DatasetDefinitionCollection> ListDefinitionsAsync(int limit = 10,
                                                               string afterName = null);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<DatasetDefinition>> ListAllDefinitionsAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="datasetName"></param>
        /// <returns></returns>
        Task DeleteDatasetAsync(string datasetName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        Task<DatasetDefinition> CreateDatasetAsync(DatasetDefinition dataset);
    }
}

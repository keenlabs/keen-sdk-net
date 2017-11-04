using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Keen.Core.Dataset
{
    public interface IDataset
    {
        Task<JObject> Results(string datasetName, string indexBy, string timeframe);
        Task<DatasetDefinition> Definition(string datasetName);
        Task<DatasetDefinitionCollection> ListDefinitions(int limit = 10, string afterName = null);
        Task<IEnumerable<DatasetDefinition>> ListAllDefinitions();
        Task DeleteDataset(string datasetName);
        Task<DatasetDefinition> CreateDataset(DatasetDefinition dataset);
    }
}

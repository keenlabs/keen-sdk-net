using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Keen.Core.Dataset
{
    using System.Collections.Generic;

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

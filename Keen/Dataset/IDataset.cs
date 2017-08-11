using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Keen.Core.Dataset
{
    public interface IDataset
    {
        Task<JObject> Results(string datasetName, string indexBy, string timeframe);
        Task<DatasetDefinition> Definition(string datasetName);
    }
}

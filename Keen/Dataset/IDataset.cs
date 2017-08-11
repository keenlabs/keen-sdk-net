namespace Keen.Core.Dataset
{
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public interface IDataset
    {
        Task<JObject> Results(string datasetName, string indexBy, string timeframe);
        Task<DatasetDefinition> Definition(string datasetName);
    }
}

using System;

namespace Keen.Core.Dataset
{
    using System.Threading.Tasks;
    using ContractResolvers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Query;

    public interface IDataset
    {
        Task<JObject> Results(string datasetName, string indexBy, string timeframe);
        Task<DatasetDefinition> Definition(string datasetName);
    }

    internal class Datasets : IDataset
    {
        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _cachedDatasetRelativeUrl;
        private readonly string _masterKey;

        internal Datasets(IProjectSettings prjSettings,
                         IKeenHttpClientProvider keenHttpClientProvider)
        {
            if (null == prjSettings)
            {
                throw new ArgumentNullException(nameof(prjSettings),
                                                "Project Settings must be provided.");
            }

            if (null == keenHttpClientProvider)
            {
                throw new ArgumentNullException(nameof(keenHttpClientProvider),
                                                "A KeenHttpClient provider must be provided.");
            }

            if (string.IsNullOrWhiteSpace(prjSettings.KeenUrl) ||
                !Uri.IsWellFormedUriString(prjSettings.KeenUrl, UriKind.Absolute))
            {
                throw new KeenException(
                    "A properly formatted KeenUrl must be provided via Project Settings.");
            }

            var serverBaseUrl = new Uri(prjSettings.KeenUrl);
            _keenHttpClient = keenHttpClientProvider.GetForUrl(serverBaseUrl);
            _cachedDatasetRelativeUrl = KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
                                                              KeenConstants.DatasetsResource);

            _masterKey = prjSettings.MasterKey;
        }

        public async Task<JObject> Results(string datasetName, string indexBy, string timeframe)
        {
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                throw new KeenException("A dataset name is required.");
            }

            if (string.IsNullOrWhiteSpace(indexBy))
            {
                throw new KeenException("A value to index by is required.");
            }

            if (string.IsNullOrWhiteSpace(timeframe))
            {
                throw new KeenException("A timeframe by is required.");
            }

            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API masterkey is required to get dataset results.");
            }

            var datasetResultsUrl = $"{this.GetDatasetUrl(datasetName)}/results";

            var url = $"{datasetResultsUrl}?index_by={indexBy}&timeframe={timeframe}";

            var responseMessage = await _keenHttpClient
                .GetAsync(url, _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMessage
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new KeenException("Request failed with status: " +
                                        responseMessage.StatusCode);
            }

            return response;
        }

        public async Task<DatasetDefinition> Definition(string datasetName)
        {
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                throw new KeenException("A dataset name is required.");
            }

            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API masterkey is required to get dataset results.");
            }

            var responseMessage = await _keenHttpClient
                .GetAsync(this.GetDatasetUrl(datasetName), _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMessage
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new KeenException("Request failed with status: " +
                                        responseMessage.StatusCode);
            }

            return JsonConvert.DeserializeObject<DatasetDefinition>(responseString, new JsonSerializerSettings { ContractResolver = new SnakeCaseContractResolver()});
        }

        private string GetDatasetUrl(string datasetName)
        {
            return $"{_cachedDatasetRelativeUrl}/{datasetName}";
        }
    }
}

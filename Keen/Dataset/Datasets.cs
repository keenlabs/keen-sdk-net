using Keen.Core.ContractResolvers;
using Keen.Core.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace Keen.Core.Dataset
{
    /// <summary>
    /// Datasets implements the IDataset interface which represents the Keen.IO Cached Datasets
    /// API methods.
    /// </summary>
    internal class Datasets : IDataset
    {
        private const int MAX_DATASET_DEFINITION_LIST_LIMIT = 100;

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
            _cachedDatasetRelativeUrl =
                KeenHttpClient.GetRelativeUrl(prjSettings.ProjectId,
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

            var datasetResultsUrl = $"{GetDatasetUrl(datasetName)}/results";

            var url = $"{datasetResultsUrl}?index_by={indexBy}&timeframe={timeframe}";

            var responseMsg = await _keenHttpClient
                .GetAsync(url, _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException($"Request failed with status: {responseMsg.StatusCode}");
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

            var responseMsg = await _keenHttpClient
                .GetAsync(GetDatasetUrl(datasetName), _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException($"Request failed with status: {responseMsg.StatusCode}");
            }

            return JsonConvert.DeserializeObject<DatasetDefinition>(responseString,
                                                                    GetSerializerSettings());
        }

        public async Task<DatasetDefinitionCollection> ListDefinitions(int limit = 10,
                                                                       string afterName = null)
        {
            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API masterkey is required to get dataset results.");
            }

            var datasetResultsUrl = $"{_cachedDatasetRelativeUrl}?limit={limit}";

            if (!string.IsNullOrWhiteSpace(afterName))
            {
                datasetResultsUrl += $"&after_name={afterName}";
            }

            var responseMsg = await _keenHttpClient
                .GetAsync(datasetResultsUrl, _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException($"Request failed with status: {responseMsg.StatusCode}");
            }

            return JsonConvert.DeserializeObject<DatasetDefinitionCollection>(
                responseString,
                GetSerializerSettings());
        }

        public async Task<IEnumerable<DatasetDefinition>> ListAllDefinitions()
        {
            var allDefinitions = new List<DatasetDefinition>();
            var firstSet = await ListDefinitions(MAX_DATASET_DEFINITION_LIST_LIMIT)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (null == firstSet?.Datasets)
            {
                throw new KeenException("Failed to fetch definition list");
            }

            if (!firstSet.Datasets.Any())
            {
                return allDefinitions;
            }

            if (firstSet.Count <= firstSet.Datasets.Count())
            {
                return firstSet.Datasets;
            }

            allDefinitions.AddRange(firstSet.Datasets);

            do
            {
                var nextSet = await ListDefinitions(MAX_DATASET_DEFINITION_LIST_LIMIT,
                                                    allDefinitions.Last().DatasetName)
                    .ConfigureAwait(continueOnCapturedContext: false);

                if (null == nextSet?.Datasets || !nextSet.Datasets.Any())
                {
                    throw new KeenException("Failed to fetch definition list");
                }

                allDefinitions.AddRange(nextSet.Datasets);
            } while (firstSet.Count > allDefinitions.Count);

            return allDefinitions;
        }

        public async Task DeleteDataset(string datasetName)
        {
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                throw new KeenException("A dataset name is required.");
            }

            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API masterkey is required to get dataset results.");
            }

            var responseMsg = await _keenHttpClient
                .DeleteAsync(GetDatasetUrl(datasetName), _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            if (HttpStatusCode.NoContent == responseMsg.StatusCode)
            {
                return;
            }

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            throw new KeenException($"Request failed with status: {responseMsg.StatusCode}");
        }

        public async Task<DatasetDefinition> CreateDataset(DatasetDefinition dataset)
        {
            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API masterkey is required to get dataset results.");
            }

            // Validate
            if (null == dataset)
            {
                throw new KeenException("An instance of DatasetDefinition must be provided");
            }

            // This throws if dataset is not valid.
            dataset.Validate();

            var content = JsonConvert.SerializeObject(dataset, GetSerializerSettings());

            var responseMsg = await _keenHttpClient
                .PutAsync(GetDatasetUrl(dataset.DatasetName), _masterKey, content)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = JObject.Parse(responseString);

            KeenUtil.CheckApiErrorCode(response);

            if (!responseMsg.IsSuccessStatusCode)
            {
                throw new KeenException($"Request failed with status: {responseMsg.StatusCode}");
            }

            return JsonConvert.DeserializeObject<DatasetDefinition>(responseString,
                                                                    GetSerializerSettings());
        }

        /* It's important that there is only one instance of JsonSerializerSettings for each
           Serialization/Desierialization to keep the DatasetDefinitionConverter and
           QueryDefinitionConverter instances threadsafe */
        private JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new SnakeCaseContractResolver(),
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                Converters =
                {
                    new DatasetDefinitionConverter(),
                    new QueryDefinitionConverter()
                }
            };
        }

        private string GetDatasetUrl(string datasetName = null)
        {
            return $"{_cachedDatasetRelativeUrl}/{datasetName}";
        }
    }
}

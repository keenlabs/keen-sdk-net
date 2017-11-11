using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Keen.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


namespace Keen.Dataset
{
    /// <summary>
    /// Datasets implements the IDataset interface which represents the Keen.IO Cached Datasets
    /// API methods.
    /// </summary>
    internal class Datasets : IDataset
    {
        private const int MaxDatasetDefinitionListLimit = 100;

        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                Formatting = Formatting.None
            };

        private readonly IKeenHttpClient _keenHttpClient;
        private readonly string _cachedDatasetRelativeUrl;
        private readonly string _masterKey;
        private readonly string _readKey;


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
            _readKey = prjSettings.ReadKey;
        }

        public async Task<JObject> GetResultsAsync(string datasetName,
                                                   string indexBy,
                                                   string timeframe)
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

            if (string.IsNullOrWhiteSpace(_readKey))
            {
                throw new KeenException("An API ReadKey is required to get dataset results.");
            }

            var datasetResultsUrl = $"{GetDatasetUrl(datasetName)}/results";

            // Absolute timeframes can have reserved characters like ':', and index_by can be
            // any valid JSON member name, which can have all sorts of stuff, so we escape here.
            var url = $"{datasetResultsUrl}?" +
                $"index_by={Uri.EscapeDataString(indexBy)}" +
                $"&timeframe={Uri.EscapeDataString(timeframe)}";

            var responseMsg = await _keenHttpClient
                .GetAsync(url, _readKey)
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

        public async Task<DatasetDefinition> GetDefinitionAsync(string datasetName)
        {
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                throw new KeenException("A dataset name is required.");
            }

            if (string.IsNullOrWhiteSpace(_readKey))
            {
                throw new KeenException("An API ReadKey is required to get dataset results.");
            }

            var responseMsg = await _keenHttpClient
                .GetAsync(GetDatasetUrl(datasetName), _readKey)
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
                                                                    SerializerSettings);
        }

        public async Task<DatasetDefinitionCollection> ListDefinitionsAsync(
            int limit = 10,
            string afterName = null)
        {
            if (string.IsNullOrWhiteSpace(_readKey))
            {
                throw new KeenException("An API ReadKey is required to get dataset results.");
            }

            // limit is just an int, so no need to encode here.
            var datasetResultsUrl = $"{_cachedDatasetRelativeUrl}?limit={limit}";

            if (!string.IsNullOrWhiteSpace(afterName))
            {
                // afterName should be a valid dataset name, which can only be
                // alphanumerics, '_' and '-', so we don't escape here.
                datasetResultsUrl += $"&after_name={afterName}";
            }

            var responseMsg = await _keenHttpClient
                .GetAsync(datasetResultsUrl, _readKey)
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

            return JsonConvert.DeserializeObject<DatasetDefinitionCollection>(responseString,
                                                                              SerializerSettings);
        }

        public async Task<IEnumerable<DatasetDefinition>> ListAllDefinitionsAsync()
        {
            var allDefinitions = new List<DatasetDefinition>();
            var firstSet = await ListDefinitionsAsync(MaxDatasetDefinitionListLimit)
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
                var nextSet = await ListDefinitionsAsync(MaxDatasetDefinitionListLimit,
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

        public async Task DeleteDatasetAsync(string datasetName)
        {
            if (string.IsNullOrWhiteSpace(datasetName))
            {
                throw new KeenException("A dataset name is required.");
            }

            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API MasterKey is required to get dataset results.");
            }

            var responseMsg = await _keenHttpClient
                .DeleteAsync(GetDatasetUrl(datasetName), _masterKey)
                .ConfigureAwait(continueOnCapturedContext: false);

            var responseString = await responseMsg
                .Content
                .ReadAsStringAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            if (HttpStatusCode.NoContent != responseMsg.StatusCode)
            {
                var response = JObject.Parse(responseString);

                KeenUtil.CheckApiErrorCode(response);

                throw new KeenException($"Request failed with status: {responseMsg.StatusCode}");
            }
        }

        public async Task<DatasetDefinition> CreateDatasetAsync(DatasetDefinition dataset)
        {
            if (string.IsNullOrWhiteSpace(_masterKey))
            {
                throw new KeenException("An API MasterKey is required to get dataset results.");
            }

            // Validate
            if (null == dataset)
            {
                throw new KeenException("An instance of DatasetDefinition must be provided");
            }

            // This throws if dataset is not valid.
            dataset.Validate();

            var content = JsonConvert.SerializeObject(dataset, SerializerSettings);

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
                                                                    SerializerSettings);
        }

        private string GetDatasetUrl(string datasetName = null)
        {
            return $"{_cachedDatasetRelativeUrl}/{datasetName}";
        }
    }
}

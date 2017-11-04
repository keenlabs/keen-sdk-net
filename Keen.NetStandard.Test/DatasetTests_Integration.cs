using Keen.Core.Dataset;
using Keen.Core.Query;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace Keen.Core.Test
{
    [TestFixture]
    public class DatasetTests_Integration : TestBase
    {
        private const string _datasetName = "video-view";
        private const string _indexBy = "12";
        private const string _timeframe = "this_12_days";


        [Test]
        public void Results_Success()
        {
            var apiResponse = File.ReadAllText($"{GetApiResponsesPath()}/GetDatasetResults.json");
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);
            var dataset = client.QueryDataset(_datasetName, _indexBy, _timeframe);

            Assert.IsNotNull(dataset);
            Assert.IsNotNull(dataset["result"]);
        }

        [Test]
        public void Definition_Success()
        {
            var apiResponse = File.ReadAllText($"{GetApiResponsesPath()}/GetDatasetDefinition.json");
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);
            var dataset = client.GetDatasetDefinition(_datasetName);

            AssertDatasetIsPopulated(dataset);
        }

        [Test]
        public void ListDefinitions_Success()
        {
            var apiResponse = File.ReadAllText($"{GetApiResponsesPath()}/ListDatasetDefinitions.json");
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);
            var datasetCollection = client.ListDatasetDefinitions();

            Assert.IsNotNull(datasetCollection);
            Assert.IsNotNull(datasetCollection.Datasets);
            Assert.IsTrue(datasetCollection.Datasets.Any());
            Assert.IsTrue(!string.IsNullOrWhiteSpace(datasetCollection.NextPageUrl));

            foreach (var item in datasetCollection.Datasets)
            {
                AssertDatasetIsPopulated(item);
            }
        }

        [Test]
        public void ListAllDefinitions_Success()
        {
            var apiResponse = File.ReadAllText($"{GetApiResponsesPath()}/ListDatasetDefinitions.json");
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);
            var datasetCollection = client.ListAllDatasetDefinitions();

            Assert.IsNotNull(datasetCollection);
            Assert.IsTrue(datasetCollection.Any());

            foreach (var item in datasetCollection)
            {
                AssertDatasetIsPopulated(item);
            }
        }

        [Test]
        public void Delete_Success()
        {
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForDeleteAsync(string.Empty);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            client.DeleteDataset("datasetName");
        }

        [Test]
        public void CreateDataset_Success()
        {
            var apiResponse = File.ReadAllText($"{GetApiResponsesPath()}/GetDatasetDefinition.json");

            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForPutAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);
            var newDataSet = CreateDatasetDefinition();
            var dataset = client.CreateDataset(newDataSet);

            AssertDatasetIsPopulated(dataset);
        }

        [Test]
        public void DatasetValidation_Throws()
        {
            var dataset = new DatasetDefinition();

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.DatasetName = "count-purchases-gte-100-by-country-daily";

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.DisplayName = "Count Daily Product Purchases Over $100 by Country";

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.IndexBy = new List<string> { "product.id" };

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.Query = new QueryDefinition();

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.Query.AnalysisType = "count";

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.Query.EventCollection = "purchases";

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.Query.Timeframe = "this_500_days";

            Assert.Throws<KeenException>(() => dataset.Validate());

            dataset.Query.Interval = "daily";

            Assert.DoesNotThrow(() => dataset.Validate());
        }

        [Test]
        public void Results_Throws()
        {
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync("{}", HttpStatusCode.InternalServerError);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            Assert.Throws<KeenException>(() => client.QueryDataset(null, _indexBy, _timeframe));
            Assert.Throws<KeenException>(() => client.QueryDataset(_datasetName, null, _timeframe));
            Assert.Throws<KeenException>(() => client.QueryDataset(_datasetName, _indexBy, null));

            Assert.Throws<KeenException>(() => client.QueryDataset(_datasetName, _indexBy, _timeframe));

            var brokenClient = new KeenClient(new ProjectSettingsProvider("5011efa95f546f2ce2000000",
                null,
                Environment.GetEnvironmentVariable("KEEN_WRITE_KEY") ?? "",
                Environment.GetEnvironmentVariable("KEEN_READ_KEY") ?? "",
                Environment.GetEnvironmentVariable("KEEN_SERVER_URL") ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/"),
                httpClientProvider);

            Assert.Throws<KeenException>(() => brokenClient.QueryDataset(_datasetName, _indexBy, _timeframe));
        }

        [Test]
        public void Definition_Throws()
        {
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync("{}", HttpStatusCode.InternalServerError);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            Assert.Throws<KeenException>(() => client.GetDatasetDefinition(null));
            Assert.Throws<KeenException>(() => client.GetDatasetDefinition(_datasetName));

            var brokenClient = new KeenClient(new ProjectSettingsProvider("5011efa95f546f2ce2000000",
                    null,
                    Environment.GetEnvironmentVariable("KEEN_WRITE_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_READ_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_SERVER_URL") ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/"),
                httpClientProvider);

            Assert.Throws<KeenException>(() => brokenClient.GetDatasetDefinition(_datasetName));
        }

        [Test]
        public void ListDefinitions_Throws()
        {
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync("{}", HttpStatusCode.InternalServerError);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            Assert.Throws<KeenException>(() => client.ListDatasetDefinitions());

            var brokenClient = new KeenClient(new ProjectSettingsProvider("5011efa95f546f2ce2000000",
                    null,
                    Environment.GetEnvironmentVariable("KEEN_WRITE_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_READ_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_SERVER_URL") ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/"),
                httpClientProvider);

            Assert.Throws<KeenException>(() => brokenClient.ListDatasetDefinitions());
        }

        [Test]
        public void DeleteDataset_Throws()
        {
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForDeleteAsync("{}", HttpStatusCode.InternalServerError);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            Assert.Throws<KeenException>(() => client.DeleteDataset(null));
            Assert.Throws<KeenException>(() => client.DeleteDataset(_datasetName));

            var brokenClient = new KeenClient(new ProjectSettingsProvider("5011efa95f546f2ce2000000",
                    null,
                    Environment.GetEnvironmentVariable("KEEN_WRITE_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_READ_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_SERVER_URL") ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/"),
                httpClientProvider);

            Assert.Throws<KeenException>(() => brokenClient.DeleteDataset(_datasetName));
        }

        [Test]
        public void CreateDataset_Throws()
        {
            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForDeleteAsync("{}", HttpStatusCode.InternalServerError);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            Assert.Throws<KeenException>(() => client.CreateDataset(null));

            var brokenClient = new KeenClient(new ProjectSettingsProvider("5011efa95f546f2ce2000000",
                    null,
                    Environment.GetEnvironmentVariable("KEEN_WRITE_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_READ_KEY") ?? "",
                    Environment.GetEnvironmentVariable("KEEN_SERVER_URL") ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/"),
                httpClientProvider);

            Assert.Throws<KeenException>(() => brokenClient.CreateDataset(CreateDatasetDefinition()));
        }

        private string GetApiResponsesPath()
        {
            var localPath = AppDomain.CurrentDomain.BaseDirectory;
            var apiResponsesPath = $"{localPath}/ApiResponses";

            return apiResponsesPath;
        }

        private IKeenHttpClientProvider GetMockHttpClientProviderForGetAsync(string response, HttpStatusCode status = HttpStatusCode.OK)
        {
            var httpResponseMessage = new HttpResponseMessage
            {
                Content = new StringContent(response),
                StatusCode = status
            };

            var mockHttpClient = new Mock<IKeenHttpClient>();
            mockHttpClient.Setup(m => m.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(httpResponseMessage));

            return new TestKeenHttpClientProvider
            {
                ProvideKeenHttpClient = (url) => mockHttpClient.Object
            };
        }

        private IKeenHttpClientProvider GetMockHttpClientProviderForPutAsync(string response)
        {
            var httpResponseMessage = new HttpResponseMessage
            {
                Content = new StringContent(response),
                StatusCode = HttpStatusCode.Created
            };

            var mockHttpClient = new Mock<IKeenHttpClient>();
            mockHttpClient.Setup(m => m.PutAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(httpResponseMessage));

            return new TestKeenHttpClientProvider
            {
                ProvideKeenHttpClient = (url) => mockHttpClient.Object
            };
        }

        private IKeenHttpClientProvider GetMockHttpClientProviderForDeleteAsync(string response, HttpStatusCode status = HttpStatusCode.NoContent)
        {
            var httpResponseMessage = new HttpResponseMessage
            {
                Content = new StringContent(response),
                StatusCode = status
            };

            var mockHttpClient = new Mock<IKeenHttpClient>();
            mockHttpClient.Setup(m => m.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(httpResponseMessage));

            return new TestKeenHttpClientProvider
            {
                ProvideKeenHttpClient = (url) => mockHttpClient.Object
            };
        }

        private void AssertDatasetIsPopulated(DatasetDefinition dataset)
        {
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.DatasetName));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.DisplayName));
            Assert.IsNotEmpty(dataset.IndexBy);

            if (UseMocks)
            {
                Assert.IsNotNull(dataset.LastScheduledDate);
                Assert.IsNotNull(dataset.LatestSubtimeframeAvailable);
            }

            Assert.IsNotNull(dataset.Query);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.ProjectId));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.AnalysisType));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.EventCollection));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.Timeframe));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.Interval));

            // TODO : We'll need to do some setup to actually get this to run automatically
            // with !UseMocks...and take into account that it can "take up to an hour for a newly
            // created dataset to compute results for the first time."
            Assert.IsNotNull(dataset.Query.GroupBy);
            Assert.IsTrue(dataset.Query.GroupBy.Count() == 1);

            if (dataset.Query.Filters != null)
            {
                foreach (var filter in dataset.Query.Filters)
                {
                    AssertFilterIsPopulated(filter);
                }
            }
        }

        private void AssertFilterIsPopulated(QueryFilter filter)
        {
            Assert.IsNotNull(filter);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(filter.PropertyName));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(filter.Operator));
        }

        private DatasetDefinition CreateDatasetDefinition()
        {
            return new DatasetDefinition
            {
                DatasetName = "count-purchases-gte-100-by-country-daily",
                DisplayName = "Count Daily Product Purchases Over $100 by Country",
                IndexBy = new List<string> { "product.id" },
                Query = new QueryDefinition
                {
                    AnalysisType = "count",
                    EventCollection = "purchases",
                    Timeframe = "this_500_days",
                    Interval = "daily"
                }
            };
        }
    }
}

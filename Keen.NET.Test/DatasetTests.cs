using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Keen.Core;
using Keen.Core.Dataset;
using Keen.Core.Query;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Keen.Net.Test
{
    using System.Net;

    [TestFixture]
    public class DatasetTests : TestBase
    {
        private const string _datasetName = "video-view";
        private const string _indexBy = "12";
        private const string _timeframe = "this_12_days";

        [Test]
        public void Results_Success()
        {
            var apiResponse = File.ReadAllText($"{this.GetLocalPath()}/ApiResponses/GetDatasetResults.json");

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
            var apiResponse = File.ReadAllText($"{this.GetLocalPath()}/ApiResponses/GetDatasetDefinition.json");

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
            var apiResponse = File.ReadAllText($"{this.GetLocalPath()}/ApiResponses/ListDatasetDefinitions.json");

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
            var apiResponse = File.ReadAllText($"{this.GetLocalPath()}/ApiResponses/ListDatasetDefinitions.json");

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
            var apiResponse = File.ReadAllText($"{this.GetLocalPath()}/ApiResponses/GetDatasetDefinition.json");

            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForPutAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            var newDataSet = new DatasetDefinition
            {
                DatasetName = "count-purchases-gte-100-by-country-daily",
                DisplayName = "Count Daily Product Purchases Over $100 by Country",
                IndexBy = "product.id",
                Query = new QueryDefinition
                {
                    AnalysisType = "count",
                    EventCollection = "purchases",
                    Timeframe = "this_500_days",
                    Interval = "daily"
                }
            };

            var dataset = client.CreateDataset(newDataSet);

            AssertDatasetIsPopulated(dataset);
        }

        private string GetLocalPath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            return new Uri(path).LocalPath;
        }

        private IKeenHttpClientProvider GetMockHttpClientProviderForGetAsync(string response)
        {
            var httpResponseMessage = new HttpResponseMessage
            {
                Content = new StringContent(response)
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

        private IKeenHttpClientProvider GetMockHttpClientProviderForDeleteAsync(string response)
        {
            var httpResponseMessage = new HttpResponseMessage
            {
                Content = new StringContent(response),
                StatusCode = HttpStatusCode.NoContent
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
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.IndexBy));
            Assert.IsNotNull(dataset.LastScheduledDate);
            Assert.IsNotNull(dataset.LatestSubtimeframeAvailable);
            Assert.IsNotNull(dataset.Query);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.ProjectId));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.AnalysisType));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.EventCollection));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.Timeframe));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.Interval));
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
    }
}

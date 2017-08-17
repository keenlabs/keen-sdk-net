namespace Keen.Net.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Core;
    using Core.Dataset;
    using Core.Query;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class DatasetTests : TestBase
    {
        private const string _datasetName = "video-view";
        private const string _indexBy = "12";
        private const string _datasetUrl = "/project/PROJECT_ID/dataset";
        private const int _listDatasetLimit = 1;

        [Test]
        public void GetDatasetResults_Success()
        {
            var timeframe = QueryRelativeTimeframe.ThisNMinutes(12);

            var result = new JObject();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.Results(
                        It.Is<string>(n => n == _datasetName),
                        It.Is<string>(i => i == _indexBy),
                        It.Is<string>(t => t == timeframe.ToString())))
                    .ReturnsAsync(result);

                client.Datasets = datasetMock.Object;
            }

            var dataset = client.QueryDataset(_datasetName, _indexBy, timeframe.ToString());
            Assert.IsNotNull(dataset);

            datasetMock?.VerifyAll();
        }

        [Test]
        public void GetDatasetDefinition_Success()
        {
            var result = new DatasetDefinition();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.Definition(
                        It.Is<string>(n => n == _datasetName)))
                    .ReturnsAsync(result);

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinition = client.GetDatasetDefinition(_datasetName);
            Assert.IsNotNull(datasetDefinition);

            datasetMock?.VerifyAll();
        }

        [Test]
        public void ListDatasetDefinitions_Success()
        {
            var result = new DatasetDefinitionCollection();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.ListDefinitions(
                        It.Is<int>(n => n == _listDatasetLimit),
                        It.Is<string>(n => n == _datasetName)))
                    .ReturnsAsync(result);

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinitionCollection = client.ListDatasetDefinitions(_listDatasetLimit, _datasetName);
            Assert.IsNotNull(datasetDefinitionCollection);

            datasetMock?.VerifyAll();
        }

        [Test]
        public void ListDatasetAllDefinitions_Success()
        {
            IEnumerable<DatasetDefinition> result = new List<DatasetDefinition>();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.ListAllDefinitions())
                    .ReturnsAsync(result);

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinitions = client.ListAllDatasetDefinitions();
            Assert.IsNotNull(datasetDefinitions);

            datasetMock?.VerifyAll();
        }

        [Test]
        public void CreateDataset_Success()
        {
            var result = new DatasetDefinition();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.CreateDataset(
                        It.Is<DatasetDefinition>(n => n != null)))
                    .ReturnsAsync(result);

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinition = client.CreateDataset(new DatasetDefinition());
            Assert.IsNotNull(datasetDefinition);

            datasetMock?.VerifyAll();
        }

        [Test]
        public void DeleteDataset_Success()
        {
            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.DeleteDataset(
                        It.Is<string>(n => n == _datasetName)))
                    .Returns(Task.Delay(0));

                client.Datasets = datasetMock.Object;
            }

            client.DeleteDataset(_datasetName);

            datasetMock?.VerifyAll();
        }

        [Test]
        public void SerializeDefinition_Success()
        {
            var apiResponse = File.ReadAllText($"{this.GetLocalPath()}/ApiResponses/GetDatasetDefinition.json");

            IKeenHttpClientProvider httpClientProvider = null;

            if (UseMocks)
            {
                httpClientProvider = GetMockHttpClientProviderForGetAsync(apiResponse);
            }

            var client = new KeenClient(SettingsEnv, httpClientProvider);

            var dataset = client.GetDatasetDefinition(_datasetName);

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
            Assert.IsNotNull(dataset.Query.Filters);
            Assert.IsTrue(dataset.Query.Filters.Count() == 1);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.Filters.FirstOrDefault().PropertyName));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(dataset.Query.Filters.FirstOrDefault().Operator));
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
    }
}

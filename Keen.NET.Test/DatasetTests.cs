namespace Keen.Net.Test
{
    using System.Collections.Generic;
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
        private const int _listDatasetLimit = 1;

        [Test]
        public async Task GetDatasetResults_Success()
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
                    .Returns(Task.FromResult(result));

                client.Datasets = datasetMock.Object;
            }

            var dataset = await client.QueryDatasetAsync(_datasetName, _indexBy, timeframe.ToString());
            Assert.IsNotNull(dataset);

            datasetMock?.VerifyAll();
        }

        [Test]
        public async Task GetDatasetDefinition_Success()
        {
            var result = new DatasetDefinition();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.Definition(
                        It.Is<string>(n => n == _datasetName)))
                    .Returns(Task.FromResult(result));

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinition = await client.GetDatasetDefinitionAsync(_datasetName);
            Assert.IsNotNull(datasetDefinition);

            datasetMock?.VerifyAll();
        }

        [Test]
        public async Task ListDatasetDefinitions_Success()
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
                    .Returns(Task.FromResult(result));

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinitionCollection = await client.ListDatasetDefinitionsAsync(_listDatasetLimit, _datasetName);
            Assert.IsNotNull(datasetDefinitionCollection);

            datasetMock?.VerifyAll();
        }

        [Test]
        public async Task ListDatasetAllDefinitions_Success()
        {
            IEnumerable<DatasetDefinition> result = new List<DatasetDefinition>();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.ListAllDefinitions())
                    .Returns(Task.FromResult(result));

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinitions = await client.ListAllDatasetDefinitionsAsync();
            Assert.IsNotNull(datasetDefinitions);

            datasetMock?.VerifyAll();
        }

        [Test]
        public async Task CreateDataset_Success()
        {
            var result = new DatasetDefinition();

            var client = new KeenClient(SettingsEnv);

            Mock<IDataset> datasetMock = null;

            if (UseMocks)
            {
                datasetMock = new Mock<IDataset>();
                datasetMock.Setup(m => m.CreateDataset(
                        It.Is<DatasetDefinition>(n => n != null)))
                    .Returns(Task.FromResult(result));

                client.Datasets = datasetMock.Object;
            }

            var datasetDefinition = await client.CreateDatasetAsync(new DatasetDefinition());
            Assert.IsNotNull(datasetDefinition);

            datasetMock?.VerifyAll();
        }

        [Test]
        public async Task DeleteDataset_Success()
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

            await client.DeleteDatasetAsync(_datasetName);

            datasetMock?.VerifyAll();
        }
    }
}

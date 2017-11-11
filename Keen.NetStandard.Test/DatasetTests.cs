using System.Collections.Generic;
using System.Threading.Tasks;
using Keen.Core;
using Keen.Dataset;
using Keen.Query;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;


namespace Keen.Test
{
    [TestFixture]
    public class DatasetTests : TestBase
    {
        private const string _datasetName = "video-view";
        private const string _indexBy = "12";
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
                datasetMock.Setup(m => m.GetResultsAsync(
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
                datasetMock.Setup(m => m.GetDefinitionAsync(
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
                datasetMock.Setup(m => m.ListDefinitionsAsync(
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
                datasetMock.Setup(m => m.ListAllDefinitionsAsync())
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
                datasetMock.Setup(m => m.CreateDatasetAsync(
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
                datasetMock.Setup(m => m.DeleteDatasetAsync(
                        It.Is<string>(n => n == _datasetName)))
                    .Returns(Task.Delay(0));

                client.Datasets = datasetMock.Object;
            }

            client.DeleteDataset(_datasetName);

            datasetMock?.VerifyAll();
        }
    }
}

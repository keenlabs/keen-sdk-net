namespace Keen.Net.Test
{
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
    }
}

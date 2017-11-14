using Keen.Query;
using Newtonsoft.Json;
using NUnit.Framework;


namespace Keen.Test
{
    [TestFixture]
    class TimeframeConverterTest
    {
        [Test]
        public void Test_TimeframeConverter_Serializes()
        {
            var timeframeString = "this_4_hours";
            var timeframe = QueryRelativeTimeframe.Create(timeframeString);

            string json = JsonConvert.SerializeObject(timeframe);

            Assert.AreEqual($"\"{timeframeString}\"", json);
        }

        [Test]
        public void Test_TimeframeConverter_Deserializes()
        {
            var timeframeString = "this_4_hours";
            var timeframeJson = $"\"{timeframeString}\"";

            var timeframe = JsonConvert.DeserializeObject<QueryRelativeTimeframe>(timeframeJson);

            Assert.AreEqual(timeframeString, timeframe.ToString());
        }
    }
}

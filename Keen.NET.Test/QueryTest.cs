using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Keen.Core;
using Keen.Core.Query;

namespace Keen.Net.Test
{
    [TestFixture]
    public class QueryTest : TestBase
    {
        const string testCol = "QueryTestCol";

        public QueryTest()
        {
            UseMocks = false;
        }

        [TestFixtureSetUp]
        public override void Setup()
        {
            base.Setup();

            // If not using mocks, set up conditions on the server
            //if (!UseMocks)
            //{
            //    var client = new KeenClient(settingsEnv);
            //    client.DeleteCollection(testCol);
            //    client.AddEvent(testCol, new { field1 = "value1" });
            //}
        }

        [Test]
        public async void AvailableQueries_Success()
        {
            var client = new KeenClient(settingsEnv);

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                // A few values that should be present and are unlikely to change
                IEnumerable<KeyValuePair<string,string>> testResult = new List<KeyValuePair<string, string>>() 
                { 
                    new KeyValuePair<string, string>("minimum", "url" ),
                    new KeyValuePair<string, string>("average", "url" ),
                    new KeyValuePair<string, string>("maximum", "url" ),
                    new KeyValuePair<string, string>("count_url", "url" ),
                };

                queryMock = new Mock<IQueries>();
                queryMock.Setup(m=>m.AvailableQueries())
                    .Returns(Task.FromResult(testResult));
                
                client.Queries = queryMock.Object;
            }

            var response = await client.GetQueries();
            Assert.True(response.Any(p => p.Key == "minimum"));
            Assert.True(response.Any(p => p.Key == "average"));
            Assert.True(response.Any(p => p.Key == "maximum"));
            Assert.True(response.Any(p => p.Key == "count_url"));
            if (null != queryMock)
                queryMock.Verify(m => m.AvailableQueries());
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(ArgumentNullException))]
        public async void Count_InvalidCollection_Throws()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousHour();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Count(
                        It.Is<string>(c => c == null),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f=>f==null)
                        ))
                        .Throws(new ArgumentNullException());

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(null, timeframe);
            Assert.IsNotNull(count);
        }

        [Test]
        public async void Count_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Count(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryAbsoluteTimeframe>(t=>t==timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f=>f==null)
                        ))
                    .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(testCol, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Count_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalCount> result =
                new List<QueryIntervalCount>() { new QueryIntervalCount(0, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Count(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.IsAny<IEnumerable<QueryFilter>>()
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryCount(testCol, timeframe, interval);
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Count_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Count(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null)
                        ))
                    .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(testCol, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.Verify(m => m.Count(
                    It.Is<string>(s => s == testCol),
                    It.Is<QueryTimeframe>(t => t == timeframe),
                    It.Is<IEnumerable<QueryFilter>>(f => f == null)
                    ),
                    Times.Once());
            }
        }

        [Test]
        public async void Count_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalCount> result =
                new List<QueryIntervalCount>() { new QueryIntervalCount(0, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Count(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null)
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryCount(testCol, timeframe, interval);
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Count_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var filters = new List<QueryFilter>(){ new QueryFilter("field1", QueryFilter.FilterOperator.gt, "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Count(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryTimeframe>(t => t == null),
                        It.Is<IEnumerable<QueryFilter>>(f => f == filters)
                        ))
                    .Returns(Task.FromResult(1));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(testCol, null, filters);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.Verify(m => m.Count(
                    It.Is<string>(s => s == testCol),
                    It.Is<QueryTimeframe>(t => t == null),
                    It.Is<IEnumerable<QueryFilter>>(f => f == filters)
                    ),
                    Times.Once());
            }
        }

    }

    [TestFixture]
    public class QueryFilterTest
    {
        [Test]
        public void Constructor_InvalidProperty_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryFilter(null, QueryFilter.FilterOperator.eq, "val"));
        }

        [Test]
        public void Constructor_InvalidValue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryFilter("prop", QueryFilter.FilterOperator.eq, null));
        }

        [Test]
        public void Constructor_ValidParams_Success()
        {
            Assert.DoesNotThrow(() => new QueryFilter("prop", QueryFilter.FilterOperator.eq, "val"));
        }

        [Test]
        public void Serialize_SimpleValue_Success()
        {
            var filter = new QueryFilter("prop", QueryFilter.FilterOperator.eq, "val");

            var json = JObject.FromObject(filter).ToString();

            var expectedJson =  "{\r\n" +
                                "  \"property_name\": \"prop\",\r\n"+
                                "  \"operator\": \"eq\",\r\n"+
                                "  \"property_value\": \"val\"\r\n"+
                                "}";
            Assert.AreEqual(json, expectedJson);
        }

        [Test]
        public void Serialize_GeoValue_Success()
        {
            var filter = new QueryFilter("prop", QueryFilter.FilterOperator.within, new QueryFilter.GeoValue(10.0, 10.0, 5.0));

            var json = JObject.FromObject(filter).ToString();
            Trace.WriteLine(json);
            var expectedJson =  "{\r\n"+
                                "  \"property_name\": \"prop\",\r\n"+
                                "  \"operator\": \"within\",\r\n"+
                                "  \"property_value\": {\r\n"+
                                "    \"coordinates\": [\r\n"+
                                "      10.0,\r\n"+
                                "      10.0\r\n"+
                                "    ],\r\n"+
                                "    \"max_distance_miles\": 5.0\r\n"+
                                "  }\r\n"+
                                "}";

            Assert.AreEqual(json, expectedJson);
        }

    }
}

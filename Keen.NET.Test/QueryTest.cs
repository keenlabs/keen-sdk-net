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
using System.Dynamic;

namespace Keen.Net.Test
{
    [TestFixture]
    public class QueryTest : TestBase
    {
        const string testCol = "QueryTestCol";

        public QueryTest()
        {
            UseMocks = true;
        }

        [TestFixtureSetUp]
        public override void Setup()
        {
            base.Setup();

            // If not using mocks, set up conditions on the server
            if (!UseMocks)
            {
                var client = new KeenClient(settingsEnv);
                //client.DeleteCollection(testCol);
                client.AddEvent(testCol, new { field1 = "99999999" });
            }
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
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == null),
                        It.Is<string>(p => p == "-"),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f=>f==null),
                        It.Is<string>(z => z == "")
                        ))
                        .Throws(new ArgumentNullException());

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(null, timeframe, null);
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
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f=>f==null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(testCol, timeframe, null);
            Assert.IsNotNull(count, "expected valid count");

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Count_ValidRelativeGroup_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousNDays(2);
            var groupby = "field1";
            IEnumerable<QueryGroupValue<int>> reply = new List<QueryGroupValue<int>>()
            {
                new QueryGroupValue<int>( 0, "field1" ),
                new QueryGroupValue<int>( 0, "field1" ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<string>(g => g == groupby),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(reply));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCountGroup(testCol, groupby, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.Verify(m => m.Metric<int>(
                    It.Is<string>(me => me == "count"),
                    It.Is<string>(c => c == testCol),
                    It.Is<string>(p => p == "-"),
                    It.Is<string>(g => g == groupby),
                    It.Is<QueryTimeframe>(t => t == timeframe),
                    It.Is<IEnumerable<QueryFilter>>(f => f == null),
                    It.Is<string>(z => z == "")),
                    Times.Once());
            }
        }

        [Test]
        public async void Count_ValidRelativeGroupInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousNDays(2);
            var interval = QueryInterval.EveryNHours(2);
            var groupby = "field1";

            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<int>>>> reply = new List<QueryIntervalValue<IEnumerable<QueryGroupValue<int>>>>()
            {
                new QueryIntervalValue<IEnumerable<QueryGroupValue<int>>>( 
                    new List<QueryGroupValue<int>>()
                    {
                        new QueryGroupValue<int>( 1, "field1" ),
                        new QueryGroupValue<int>( 1, "field1" ),
                    }, 
                    DateTime.Now, DateTime.Now.AddSeconds(2)
                ),
                new QueryIntervalValue<IEnumerable<QueryGroupValue<int>>>( 
                    new List<QueryGroupValue<int>>()
                    {
                        new QueryGroupValue<int>( 2, "field1" ),
                        new QueryGroupValue<int>( 2, "field1" ),
                    }, 
                    DateTime.Now, DateTime.Now.AddSeconds(2)
                ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<string>(g => g == groupby),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(reply));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCountIntervalGroup(testCol, groupby, timeframe, interval);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.Verify(m => m.Metric<int>(
                    It.Is<string>(me => me == "count"),
                    It.Is<string>(c => c == testCol),
                    It.Is<string>(p => p == "-"),
                    It.Is<string>(g => g == groupby),
                    It.Is<QueryTimeframe>(t => t == timeframe),
                    It.Is<QueryInterval>(i => i == interval),
                    It.Is<IEnumerable<QueryFilter>>(f => f == null),
                    It.Is<string>(z => z == "")),
                    Times.Once());
            }
        }


        [Test]
        public async void Count_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<int>> result =
                new List<QueryIntervalValue<int>>() { new QueryIntervalValue<int>(0, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.IsAny<IEnumerable<QueryFilter>>(),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryCountInterval(testCol, timeframe, interval);
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
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(testCol, timeframe, null);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.Verify(m => m.Metric<int>(
                    It.Is<string>(me => me == "count"),
                    It.Is<string>(c => c == testCol),
                    It.Is<string>(p => p == "-"),
                    It.Is<QueryTimeframe>(t => t == timeframe),
                    It.Is<IEnumerable<QueryFilter>>(f => f == null),
                    It.Is<string>(z => z == "")),
                    Times.Once());
            }
        }

        [Test]
        public async void Count_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<int>> result =
                new List<QueryIntervalValue<int>>() { new QueryIntervalValue<int>(0, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryCountInterval(testCol, timeframe, interval);
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Count_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var filters = new List<QueryFilter>(){ new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == "-"),
                        It.Is<QueryTimeframe>(t => t == null),
                        It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(1));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCount(testCol, null, filters);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.Verify(m => m.Metric<int>(
                    It.Is<string>(me => me == "count"),
                    It.Is<string>(c => c == testCol),
                    It.Is<string>(p => p == "-"),
                    It.Is<QueryTimeframe>(t => t == null),
                    It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                    It.Is<string>(z => z == "")),
                    Times.Once());
            }
        }


        [Test]
        public async void CountUnique_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p=> p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t=>t=="")
                        ))
                    .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCountUnique(testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void CountUnique_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCountUnique(testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void CountUnique_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count_unique"),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(0));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryCountUnique(testCol, prop, null, filters);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void CountUnique_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<int>> result =
                new List<QueryIntervalValue<int>>() { new QueryIntervalValue<int>(0, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p=> p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i=>i==interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t=>t=="")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryCountUniqueInterval(testCol, prop, timeframe, interval);
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void CountUnique_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<int>> result =
                new List<QueryIntervalValue<int>>() { new QueryIntervalValue<int>(0, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<int>(
                        It.Is<string>(me => me == "count_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryCountUniqueInterval(testCol, prop, timeframe, interval);
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Minimum_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "minimum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryMinimum(testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Minimum_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "minimum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            await client.QueryMinimum(testCol, prop, timeframe);            

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Minimum_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                      It.Is<string>(me => me == "minimum"),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            await client.QueryMinimum(testCol, prop, null, filters);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Minimum_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>("0", timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "minimum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryMinimumInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Minimum_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>("0", DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "minimum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QueryMinimumInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Maximum_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "maximum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryMaximum(testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Maximum_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "maximum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            await client.QueryMaximum(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Maximum_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                      It.Is<string>(me => me == "maximum"),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            await client.QueryMaximum(testCol, prop, null, filters);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Maximum_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>("0", timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "maximum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryMaximumInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Maximum_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>("0", DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<string>(
                        It.Is<string>(me => me == "maximum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QueryMaximumInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Average_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                        It.Is<string>(me => me == "average"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult((double?)0.0));

                client.Queries = queryMock.Object;
            }

            await client.QueryAverage(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Average_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                      It.Is<string>(me => me == "average"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult((double?)0.0));

                client.Queries = queryMock.Object;
            }

            await client.QueryAverage(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Average_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                      It.Is<string>(me => me == "average"),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult((double?)0.0));

                client.Queries = queryMock.Object;
            }

            await client.QueryAverage(testCol, prop, null, filters);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Average_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<double?>> result =
                new List<QueryIntervalValue<double?>>() { new QueryIntervalValue<double?>(0.0, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                        It.Is<string>(me => me == "average"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QueryAverageInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Average_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<double?>> result =
                new List<QueryIntervalValue<double?>>() { new QueryIntervalValue<double?>((double?)0.0, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                      It.Is<string>(me => me == "average"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QueryAverageInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }


        [Test]
        public async void Sum_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                        It.Is<string>(me => me == "sum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult((double?)0.0));

                client.Queries = queryMock.Object;
            }

            await client.QuerySum(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Sum_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                        It.Is<string>(me => me == "sum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult((double?)0.0));

                client.Queries = queryMock.Object;
            }

            await client.QuerySum(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Sum_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                      It.Is<string>(me => me == "sum"),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult((double?)0.0));

                client.Queries = queryMock.Object;
            }

            await client.QuerySum(testCol, prop, null, filters);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Sum_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<double?>> result =
                new List<QueryIntervalValue<double?>>() { new QueryIntervalValue<double?>(0.0, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                        It.Is<string>(me => me == "sum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QuerySumInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Sum_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<double?>> result =
                new List<QueryIntervalValue<double?>>() { new QueryIntervalValue<double?>((double?)0.0, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<double?>(
                        It.Is<string>(me => me == "sum"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QuerySumInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }


        [Test]
        public async void SelectUnique_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";
            IEnumerable<string> result = new List<string>() { "hello", "goodbye", "I'm late" };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                        It.Is<string>(me => me == "select_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.QuerySelectUnique(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void SelectUnique_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<string> result = new List<string>() { "hello", "goodbye", "I'm late" };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                        It.Is<string>(me => me == "select_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QuerySelectUnique(testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void SelectUnique_ValidRelativeGroup_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var groupby = "field1";
            var timeframe = QueryRelativeTimeframe.PreviousNDays(5);
            IEnumerable<QueryGroupValue<IEnumerable<string>>> reply = new List<QueryGroupValue<IEnumerable<string>>>()
            {
                new QueryGroupValue<IEnumerable<string>>( new List<string>() { "hello", "goodbye", "I'm late" }, "field1" ),
                new QueryGroupValue<IEnumerable<string>>( new List<string>() { "hello", "goodbye", "I'm late" }, "field1" ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                        It.Is<string>(me => me == "select_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<string>(g => g == groupby),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(reply));

                client.Queries = queryMock.Object;
            }

            await client.QuerySelectUniqueGroup(testCol, prop, groupby, timeframe, null);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void SelectUnique_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };
            IEnumerable<string> result = new List<string>() { "hello", "goodbye", "I'm late" };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                      It.Is<string>(me => me == "select_unique"),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QuerySelectUnique(testCol, prop, null, filters);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void SelectUnique_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<string> resultl = new List<string>() { "hello", "goodbye", "I'm late" };
            IEnumerable<QueryIntervalValue<IEnumerable<string>>> result =
                new List<QueryIntervalValue<IEnumerable<string>>>() { new QueryIntervalValue<IEnumerable<string>>(resultl, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                        It.Is<string>(me => me == "select_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QuerySelectUniqueInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void SelectUnique_ValidAbsoluteIntervalGroup_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNHours(4);
            var groupby = "field1";
            IEnumerable<string> resultl = new List<string>() { "hello", "goodbye", "I'm late" };

            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<IEnumerable<string>>>>> result =
                new List<QueryIntervalValue<IEnumerable<QueryGroupValue<IEnumerable<string>>>>>() 
                { 
                    new QueryIntervalValue<IEnumerable<QueryGroupValue<IEnumerable<string>>>>(
                        new List<QueryGroupValue<IEnumerable<string>>>(){
                            new QueryGroupValue<IEnumerable<string>>(resultl, "abc"),
                            new QueryGroupValue<IEnumerable<string>>(resultl, "def")
                        }, 
                        timeframe.Start, timeframe.End
                        ),
                    new QueryIntervalValue<IEnumerable<QueryGroupValue<IEnumerable<string>>>>(
                        new List<QueryGroupValue<IEnumerable<string>>>(){
                            new QueryGroupValue<IEnumerable<string>>(resultl, "abc"),
                            new QueryGroupValue<IEnumerable<string>>(resultl, "def")
                        }, 
                        timeframe.Start, timeframe.End
                        ),
                };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                        It.Is<string>(me => me == "select_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<string>(g => g == groupby),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = await client.QuerySelectUniqueIntervalGroup(testCol, prop, groupby, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }


        [Test]
        public async void SelectUnique_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<string> resultl = new List<string>() { "hello", "goodbye", "I'm late" };
            IEnumerable<QueryIntervalValue<IEnumerable<string>>> result =
                new List<QueryIntervalValue<IEnumerable<string>>>() { new QueryIntervalValue<IEnumerable<string>>(resultl, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric<IEnumerable<string>>(
                        It.Is<string>(me => me == "select_unique"),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.QuerySelectUniqueInterval(testCol, prop, timeframe, interval);

            if (null != queryMock)
                queryMock.VerifyAll();
        }




        [Test]
        public async void ExtractResource_ValidAbsolute_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            dynamic eo = new ExpandoObject();
            eo.field1 = "8888";
            IEnumerable<dynamic> result = new List<dynamic>() { eo, eo };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Extract(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<int>( l => l == 0),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.ExtractResource(testCol, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void ExtractResource_ValidRelative_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            dynamic eo = new ExpandoObject();
            eo.field1 = "8888";
            IEnumerable<dynamic> result = new List<dynamic>() { eo, eo };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Extract(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<int>(l => l == 0),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.ExtractResource(testCol, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void ExtractResource_ValidFilter_Success()
        {
            var client = new KeenClient(settingsEnv);
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };
            dynamic eo = new ExpandoObject();
            eo.field1 = "8888";
            IEnumerable<dynamic> result = new List<dynamic>() { eo, eo };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Extract(
                        It.Is<string>(c => c == testCol),
                        It.Is<QueryAbsoluteTimeframe>(t => t == null),
                        It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                        It.Is<int>(l => l == 0),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.ExtractResource(testCol, null, filters);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void Funnel_ValidTimeframe_Success()
        {
            var client = new KeenClient(settingsEnv);
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            var funnelColA = "FunnelTestA";
            var funnelColB = "FunnelTestB";
            var funnelColC = "FunnelTestC";

            try
            {
                if (!UseMocks)
                {
                    client.DeleteCollection(funnelColA);
                    client.DeleteCollection(funnelColB);
                    client.DeleteCollection(funnelColC);
                }
            }
            catch (Exception)
            {
            }

            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now, DateTime.Now.AddSeconds(2));

            if (!UseMocks)
            {
                client.AddEvent(funnelColA, new { id = "1" });
                client.AddEvent(funnelColA, new { id = "2" });
                client.AddEvent(funnelColA, new { id = "3" });

                client.AddEvent(funnelColB, new { id = "1" });
                client.AddEvent(funnelColB, new { id = "2" });

                client.AddEvent(funnelColC, new { id = "1" });
            }
            IEnumerable<FunnelStep> funnelsteps = new List<FunnelStep>()
            {
                new FunnelStep(){ EventCollection = funnelColA, ActorProperty = "id" },
                new FunnelStep(){ EventCollection = funnelColB, ActorProperty = "id" },
                new FunnelStep(){ EventCollection = funnelColC, ActorProperty = "id" },
            };
            IEnumerable<int> result = new List<int>() { 3, 2, 1 };


            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.IsAny<string>(),
                        It.Is<IEnumerable<FunnelStep>>(f => f == funnelsteps),
                        It.Is<QueryTimeframe>(t => t == null),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }
            
            var reply = await client.Funnel(testCol, funnelsteps, null);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void MultiAnalysis_Valid_Success()
        {
            var client = new KeenClient(settingsEnv);
            IEnumerable<MultiAnalysisParam> param = new List<MultiAnalysisParam>() 
            { 
                new MultiAnalysisParam("first", MultiAnalysisParam.Metric.Count()),
                new MultiAnalysisParam("second", MultiAnalysisParam.Metric.Maximum("field1")),
                new MultiAnalysisParam("third", MultiAnalysisParam.Metric.Minimum("field1")),
            };
            IDictionary<string, string> result = new Dictionary<string, string>();
            result.Add("second", "fff");
            result.Add("third", "aaa");
            result.Add("first", "123");

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.MultiAnalysis(
                        It.Is<string>(c => c == testCol),
                        It.Is<IEnumerable<MultiAnalysisParam>>(p => p == param),
                        It.Is<QueryTimeframe>(t => t == null),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(tz => tz == "")
                      ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.MultiAnalysis(testCol, param, null, null, "");

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async void MultiAnalysis_ValidRelativeTimeFrame_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousNDays(2);
            IEnumerable<MultiAnalysisParam> param = new List<MultiAnalysisParam>() 
            { 
                new MultiAnalysisParam("first", MultiAnalysisParam.Metric.Count()),
                new MultiAnalysisParam("second", MultiAnalysisParam.Metric.Maximum("field1")),
                new MultiAnalysisParam("third", MultiAnalysisParam.Metric.Minimum("field1")),
            };
            IDictionary<string, string> result = new Dictionary<string, string>();
            result.Add("second", "fff");
            result.Add("third", "aaa");
            result.Add("first", "123");

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.MultiAnalysis(
                        It.Is<string>(c => c == testCol),
                        It.Is<IEnumerable<MultiAnalysisParam>>(p => p == param),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(tz => tz == "")
                      ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.MultiAnalysis(testCol, param, timeframe, null, "");

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async void MultiAnalysis_ValidGroupBy_Success()
        {
            var client = new KeenClient(settingsEnv);
            var groupby = "field1";
            IEnumerable<MultiAnalysisParam> param = new List<MultiAnalysisParam>() 
            { 
                new MultiAnalysisParam("first", MultiAnalysisParam.Metric.Count()),
                new MultiAnalysisParam("second", MultiAnalysisParam.Metric.Maximum("field1")),
                new MultiAnalysisParam("third", MultiAnalysisParam.Metric.Minimum("field1")),
            };
            var dict = new Dictionary<string, string>();
            dict.Add("second", "fff");
            dict.Add("third", "aaa");
            dict.Add("first", "123");
            dict.Add(groupby, "123");
            IEnumerable<QueryGroupValue<IDictionary<string, string>>> result = new List<QueryGroupValue<IDictionary<string, string>>>()
            {
                new QueryGroupValue<IDictionary<string,string>>(dict, groupby),
                new QueryGroupValue<IDictionary<string,string>>(dict, groupby),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.MultiAnalysis(
                        It.Is<string>(c => c == testCol),
                        It.Is<IEnumerable<MultiAnalysisParam>>(p => p == param),
                        It.Is<QueryTimeframe>(t => t == null),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(g => g == groupby),
                        It.Is<string>(tz => tz == "")
                      ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.MultiAnalysis(testCol, param, null, null, groupby, "");

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async void MultiAnalysis_ValidIntervalGroupBy_Success()
        {
            var client = new KeenClient(settingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousNDays(3);
            var interval = QueryInterval.Daily();
            var groupby = "field1";
            IEnumerable<MultiAnalysisParam> param = new List<MultiAnalysisParam>() 
            { 
                new MultiAnalysisParam("first", MultiAnalysisParam.Metric.Count()),
                new MultiAnalysisParam("second", MultiAnalysisParam.Metric.Maximum("field1")),
                new MultiAnalysisParam("third", MultiAnalysisParam.Metric.Minimum("field1")),
            };

            var dict = new Dictionary<string, string>();
            dict.Add("second", "fff");
            dict.Add("third", "aaa");
            dict.Add("first", "123");
            dict.Add(groupby, "123");
            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>> result =
                new List<QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>>()
            {
                new QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>(
                    new List<QueryGroupValue<IDictionary<string,string>>>(){
                        new QueryGroupValue<IDictionary<string,string>>(dict, groupby),
                        new QueryGroupValue<IDictionary<string,string>>(dict, groupby)
                    }, 
                    DateTime.Now, DateTime.Now.AddSeconds(2)
                ),
                new QueryIntervalValue<IEnumerable<QueryGroupValue<IDictionary<string, string>>>>(
                    new List<QueryGroupValue<IDictionary<string,string>>>(){
                        new QueryGroupValue<IDictionary<string,string>>(dict, groupby),
                        new QueryGroupValue<IDictionary<string,string>>(dict, groupby)
                    }, 
                    DateTime.Now, DateTime.Now.AddSeconds(2)
                ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.MultiAnalysis(
                        It.Is<string>(c => c == testCol),
                        It.Is<IEnumerable<MultiAnalysisParam>>(p => p == param),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i=>i==interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(g => g == groupby),
                        It.Is<string>(tz => tz == "")
                      ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.MultiAnalysis(testCol, param, timeframe, interval, null, groupby, "");

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async void MultiAnalysis_ValidInterval_Success()
        {
            var client = new KeenClient(settingsEnv);
            IEnumerable<MultiAnalysisParam> param = new List<MultiAnalysisParam>() 
            { 
                new MultiAnalysisParam("first", MultiAnalysisParam.Metric.Count()),
                new MultiAnalysisParam("second", MultiAnalysisParam.Metric.Maximum("field1")),
                new MultiAnalysisParam("third", MultiAnalysisParam.Metric.Minimum("field1")),
            };
            var timeframe = QueryRelativeTimeframe.PreviousNDays(3);
            var interval = QueryInterval.Daily();
            IEnumerable<QueryIntervalValue<IDictionary<string, string>>> result = new List<QueryIntervalValue<IDictionary<string, string>>>();
            foreach( var i in Enumerable.Range(1,3))
            {
                var dic = new Dictionary<string, string>();
                dic.Add("second", "fff");
                dic.Add("third", "aaa");
                dic.Add("first", "123");

                var qv = new QueryIntervalValue<IDictionary<string,string>>(dic, DateTime.Now, DateTime.Now.AddSeconds(2));
                ((List<QueryIntervalValue<IDictionary<string, string>>>)result).Add(qv);
            }

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.MultiAnalysis(
                        It.Is<string>(c => c == testCol),
                        It.Is<IEnumerable<MultiAnalysisParam>>(p => p == param),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(tz => tz == "")
                      ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.MultiAnalysisSeries(testCol, param, timeframe, interval);

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

    }

    [TestFixture]
    public class QueryFilterTest
    {
        [Test]
        public void Constructor_InvalidProperty_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryFilter(null, QueryFilter.FilterOperator.Equals(), "val"));
        }

        [Test]
        public void Constructor_InvalidValue_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryFilter("prop", QueryFilter.FilterOperator.Equals(), null));
        }

        [Test]
        public void Constructor_ValidParams_Success()
        {
            Assert.DoesNotThrow(() => new QueryFilter("prop", QueryFilter.FilterOperator.Equals(), "val"));
        }

        [Test]
        public void Serialize_SimpleValue_Success()
        {
            var filter = new QueryFilter("prop", QueryFilter.FilterOperator.Equals(), "val");

            var json = JObject.FromObject(filter).ToString();

            var expectedJson =  "{\r\n" +
                                "  \"property_name\": \"prop\",\r\n"+
                                "  \"operator\": \"eq\",\r\n"+
                                "  \"property_value\": \"val\"\r\n"+
                                "}";
            Assert.AreEqual(expectedJson, json);
        }

        [Test]
        public void Serialize_GeoValue_Success()
        {
            var filter = new QueryFilter("prop", QueryFilter.FilterOperator.Within(), new QueryFilter.GeoValue(10.0, 10.0, 5.0));

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

            Assert.AreEqual(expectedJson, json);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

        [OneTimeSetUp]
        public override void Setup()
        {
            base.Setup();

            // If not using mocks, set up conditions on the server
            if (!UseMocks)
            {
                var client = new KeenClient(SettingsEnv);
                //client.DeleteCollection(testCol);
                client.AddEvent(testCol, new { field1 = "99999999" });
            }
        }

        [Test]
        public void ReadKeyOnly_Success()
        {
            var settings = new ProjectSettingsProvider(SettingsEnv.ProjectId, readKey: SettingsEnv.ReadKey); 
            var client = new KeenClient(settings);

            if (!UseMocks)
            {
                // Server is required for this test
                // Also, test depends on existance of collection "AddEventTest"
                Assert.DoesNotThrow(() => client.Query(QueryType.Count(), "AddEventTest", ""));
            }
        }

        [Test]
        public async Task AvailableQueries_Success()
        {
            var client = new KeenClient(SettingsEnv);

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
        public void Query_InvalidCollection_Throws()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousHour();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == null),
                        It.Is<string>(p => p == ""),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")
                        ))
                        .Throws(new ArgumentNullException());

                client.Queries = queryMock.Object;
            }

            Assert.ThrowsAsync<ArgumentNullException>( () => client.QueryAsync(QueryType.Count(), null, "", timeframe, null));
        }

        [Test]
        public async Task Query_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f=>f==null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryAsync(QueryType.Count(), testCol, "", timeframe, null);
            Assert.IsNotNull(count, "expected valid count");

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Query_ValidRelativeGroup_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousNDays(2);
            var groupby = "field1";
            IEnumerable<QueryGroupValue<string>> reply = new List<QueryGroupValue<string>>()
            {
                new QueryGroupValue<string>( "0", "field1" ),
                new QueryGroupValue<string>( "0", "field1" ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<string>(g => g == groupby),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(reply));

                client.Queries = queryMock.Object;
            }

            var count = (await client.QueryGroupAsync(QueryType.Count(), testCol, "", groupby, timeframe)).ToList();
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Query_ValidRelativeGroupInterval_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.PreviousNDays(2);
            var interval = QueryInterval.EveryNHours(2);
            var groupby = "field1";

            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>> reply = new List<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>>()
            {
                new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>( 
                    new List<QueryGroupValue<string>>()
                    {
                        new QueryGroupValue<string>( "1", "field1" ),
                        new QueryGroupValue<string>( "1", "field1" ),
                    }, 
                    DateTime.Now, DateTime.Now.AddSeconds(2)
                ),
                new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>( 
                    new List<QueryGroupValue<string>>()
                    {
                        new QueryGroupValue<string>( "2", "field1" ),
                        new QueryGroupValue<string>( "2", "field1" ),
                    }, 
                    DateTime.Now, DateTime.Now.AddSeconds(2)
                ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<string>(g => g == groupby),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(reply));

                client.Queries = queryMock.Object;
            }

            var count = (await client.QueryIntervalGroupAsync(QueryType.Count(), testCol, "", groupby, timeframe, interval)).ToList();
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.VerifyAll();
            }
        }


        [Test]
        public async Task Query_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>("0", timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.IsAny<IEnumerable<QueryFilter>>(),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = (await client.QueryIntervalAsync(QueryType.Count(), testCol, "", timeframe, interval)).ToList();
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Query_ValidRelative_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisMinute();

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryAsync(QueryType.Count(), testCol, "", timeframe, null);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async Task Query_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>("0", DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<QueryTimeframe>(t => t == timeframe),
                        It.Is<QueryInterval>(i => i == interval),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var counts = (await client.QueryIntervalAsync(QueryType.Count(), testCol, "", timeframe, interval)).ToList();
            Assert.IsNotNull(counts);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Query_ValidFilter_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var filters = new List<QueryFilter>(){ new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Count()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == ""),
                        It.Is<QueryTimeframe>(t => t == null),
                        It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                        It.Is<string>(z => z == "")))
                    .Returns(Task.FromResult("1"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryAsync(QueryType.Count(), testCol, "", null, filters);
            Assert.IsNotNull(count);

            if (null != queryMock)
            {
                queryMock.VerifyAll();
            }
        }


        [Test]
        public async Task CountUnique_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.CountUnique()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p=> p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t=>t=="")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryAsync(QueryType.CountUnique(), testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Minimum_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Minimum()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryAsync(QueryType.Minimum(), testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }


        [Test]
        public async Task Maximum_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Maximum()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0"));

                client.Queries = queryMock.Object;
            }

            var count = await client.QueryAsync(QueryType.Maximum(), testCol, prop, timeframe);
            Assert.IsNotNull(count);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Average_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Average()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0.0"));

                client.Queries = queryMock.Object;
            }

            await client.QueryAsync(QueryType.Average(), testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }



        [Test]
        public async Task Sum_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.Sum()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult("0.0"));

                client.Queries = queryMock.Object;
            }

            await client.QueryAsync(QueryType.Sum(), testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task SelectUnique_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var prop = "field1";
            var result = "hello,goodbye,I'm late";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.SelectUnique()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryAbsoluteTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = await client.QueryAsync(QueryType.SelectUnique(), testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task SelectUnique_ValidRelative_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var prop = "field1";
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            var result = "hello,goodbye,I'm late";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.SelectUnique()),
                        It.Is<string>(c => c == testCol),
                        It.Is<string>(p => p == prop),
                        It.Is<QueryRelativeTimeframe>(t => t == timeframe),
                        It.Is<IEnumerable<QueryFilter>>(f => f == null),
                        It.Is<string>(t => t == "")
                        ))
                    .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            await client.QueryAsync(QueryType.SelectUnique(), testCol, prop, timeframe);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task SelectUnique_ValidRelativeGroup_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var prop = "field1";
            var groupby = "field1";
            var timeframe = QueryRelativeTimeframe.PreviousNDays(5);
            IEnumerable<QueryGroupValue<string>> reply = new List<QueryGroupValue<string>>()
            {
                new QueryGroupValue<string>( "hello,goodbye,I'm late", "field1" ),
                new QueryGroupValue<string>( "hello,goodbye,I'm late", "field1" ),
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.SelectUnique()),
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

            (await client.QueryGroupAsync(QueryType.SelectUnique(), testCol, prop, groupby, timeframe, null)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task SelectUnique_ValidFilter_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var prop = "field1";
            var filters = new List<QueryFilter>() { new QueryFilter("field1", QueryFilter.FilterOperator.GreaterThan(), "1") };
            var result = "hello,goodbye,I'm late";

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                      It.Is<QueryType>(q => q == QueryType.SelectUnique()),
                      It.Is<string>(c => c == testCol),
                      It.Is<string>(p => p == prop),
                      It.Is<QueryRelativeTimeframe>(t => t == null),
                      It.Is<IEnumerable<QueryFilter>>(f => f == filters),
                      It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(result));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryAsync(QueryType.SelectUnique(), testCol, prop, null, filters)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task SelectUnique_ValidAbsoluteInterval_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNMinutes(5);
            var resultl = "hello,goodbye,I'm late";
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>(resultl, timeframe.Start, timeframe.End) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.SelectUnique()),
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

            var counts = (await client.QueryIntervalAsync(QueryType.SelectUnique(), testCol, prop, timeframe, interval)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task SelectUnique_ValidAbsoluteIntervalGroup_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var prop = "field1";
            var timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-1), DateTime.Now);
            var interval = QueryInterval.EveryNHours(4);
            var groupby = "field1";
            var resultl = "hello,goodbye,I'm late";

            IEnumerable<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>> result =
                new List<QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>>() 
                { 
                    new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>(
                        new List<QueryGroupValue<string>>(){
                            new QueryGroupValue<string>(resultl, "abc"),
                            new QueryGroupValue<string>(resultl, "def")
                        }, 
                        timeframe.Start, timeframe.End
                        ),
                    new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>(
                        new List<QueryGroupValue<string>>(){
                            new QueryGroupValue<string>(resultl, "abc"),
                            new QueryGroupValue<string>(resultl, "def")
                        }, 
                        timeframe.Start, timeframe.End
                        ),
                };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.SelectUnique()),
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

            var counts = (await client.QueryIntervalGroupAsync(QueryType.SelectUnique(), testCol, prop, groupby, timeframe, interval)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }


        [Test]
        public async Task SelectUnique_ValidRelativeInterval_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var prop = "field1";
            var interval = QueryInterval.EveryNMinutes(5);
            var timeframe = QueryRelativeTimeframe.ThisMinute();
            var resultl = "hello,goodbye,I'm late";
            IEnumerable<QueryIntervalValue<string>> result =
                new List<QueryIntervalValue<string>>() { new QueryIntervalValue<string>(resultl, DateTime.Now.AddMinutes(-5), DateTime.Now) };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Metric(
                        It.Is<QueryType>(q => q == QueryType.SelectUnique()),
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

            var reply = (await client.QueryIntervalAsync(QueryType.SelectUnique(), testCol, prop, timeframe, interval)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }




        [Test]
        public async Task ExtractResource_ValidAbsolute_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = (await client.QueryExtractResourceAsync(testCol, timeframe)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task ExtractResource_ValidRelative_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = (await client.QueryExtractResourceAsync(testCol, timeframe)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task ExtractResource_ValidFilter_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = (await client.QueryExtractResourceAsync(testCol, null, filters)).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }


        [Test]
        public async Task MultiAnalysis_Valid_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = await client.QueryMultiAnalysisAsync(testCol, param, null, null, "");

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async Task MultiAnalysis_ValidRelativeTimeFrame_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = await client.QueryMultiAnalysisAsync(testCol, param, timeframe, null, "");

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async Task MultiAnalysis_ValidGroupBy_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = (await client.QueryMultiAnalysisGroupAsync(testCol, param, null, null, groupby, "")).ToList();

            if (null != queryMock)
            {
                Assert.AreEqual(reply.Count(), result.Count());
                queryMock.VerifyAll();
            }
        }

        [Test]
        public async Task MultiAnalysis_ValidIntervalGroupBy_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = (await client.QueryMultiAnalysisIntervalGroupAsync(testCol, param, timeframe, interval, null, groupby, "")).ToList();

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task MultiAnalysis_ValidInterval_Success()
        {
            var client = new KeenClient(SettingsEnv);
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

            var reply = (await client.QueryMultiAnalysisIntervalAsync(testCol, param, timeframe, interval)).ToList();

            if (null != queryMock)
            {
                queryMock.VerifyAll();
                Assert.AreEqual(reply.Count(), result.Count());
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

            const string expectedJson = "{\r\n" +
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
            const string expectedJson = "{\r\n"+
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

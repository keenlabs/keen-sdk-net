using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Keen.Core;
using Keen.Query;
using Moq;
using NUnit.Framework;


namespace Keen.Test
{
    [TestFixture]
    public class FunnelTest : TestBase
    {
        public FunnelTest()
        {
            UseMocks = true;
        }

        const string FunnelColA = "FunnelTestA";
        const string FunnelColB = "FunnelTestB";
        const string FunnelColC = "FunnelTestC";

        [OneTimeSetUp]
        public override void Setup()
        {
            base.Setup();

            // If not using mocks, set up conditions on the server
            if (!UseMocks)
            {
                var client = new KeenClient(SettingsEnv);

                client.DeleteCollection(FunnelColA);
                client.DeleteCollection(FunnelColB);
                client.DeleteCollection(FunnelColC);

                client.AddEvent(FunnelColA, new { id = 1, name = new { first = "sam", last = "w" } });
                client.AddEvent(FunnelColA, new { id = 2, name = new { first = "dean", last = "w" } });
                client.AddEvent(FunnelColA, new { id = 3, name = new { first = "crowly", last = "" } });

                client.AddEvent(FunnelColB, new { id = 1, name = new { first = "sam", last = "w" } });
                client.AddEvent(FunnelColB, new { id = 2, name = new { first = "dean", last = "w" } });

                client.AddEvent(FunnelColC, new { id = 1, name = new { first = "sam", last = "w" } });

                Thread.Sleep(8000); // Give it a moment to show up. Queries will fail if run too soon.
            }
        }

        [Test]
        public async Task Funnel_Simple_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisHour();

            IEnumerable<FunnelStep> funnelsteps = new List<FunnelStep>
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id"
                },
            };

            var expected = new FunnelResult
            {
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                },
                Result = new[] { 3, 2 }
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => t == timeframe),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, timeframe));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Result);
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 2);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Funnel_Inverted_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisHour();

            IEnumerable<FunnelStep> funnelsteps = new List<FunnelStep>
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id",
                    Inverted = true
                },
            };

            var expected = new FunnelResult
            {
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                },
                Result = new[] { 3, 1 }
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => t == timeframe),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, timeframe));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Result);
            Assert.True(reply.Result.SequenceEqual(expected.Result));
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 2);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Funnel_Optional_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisHour();

            IEnumerable<FunnelStep> funnelsteps = new[]
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id",
                    Optional = true,
                },
                new FunnelStep
                {
                    EventCollection = FunnelColC,
                    ActorProperty = "id"
                },
            };

            var expected = new FunnelResult
            {
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColC,
                    },
                },
                Result = new[] { 3, 2, 1 }
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => t == timeframe),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, timeframe));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Result);
            Assert.True(reply.Result.SequenceEqual(expected.Result));
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 3);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Funnel_ValidFilter_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisHour();
            var filters = new List<QueryFilter> { new QueryFilter("id", QueryFilter.FilterOperator.GreaterThanOrEqual(), 0) };

            IEnumerable<FunnelStep> funnelsteps = new[]
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                    Filters = filters,
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id"
                },
            };

            var expected = new FunnelResult
            {
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                        Filters = filters
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                },
                Result = new[] { 3, 2 }
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => t == timeframe),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, timeframe));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Result);
            Assert.True(reply.Result.SequenceEqual(expected.Result));
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 2);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Funnel_ValidTimeframe_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisHour();

            IEnumerable<FunnelStep> funnelsteps = new[]
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id",
                },
            };

            var expected = new FunnelResult
            {
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                },
                Result = new[] { 3, 2 }
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => t == timeframe),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, timeframe));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Result);
            Assert.True(reply.Result.SequenceEqual(expected.Result));
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 2);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Funnel_ValidTimeframeInSteps_Success()
        {
            var client = new KeenClient(SettingsEnv);

            IEnumerable<FunnelStep> funnelsteps = new[]
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                    Timeframe = QueryRelativeTimeframe.ThisMonth(),
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id",
                    Timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddDays(-30), DateTime.Now),
                },
            };

            var expected = new FunnelResult
            {
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                },
                Result = new[] { 3, 2 }
            };

            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => null == t),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, null));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Result);
            Assert.True(reply.Result.SequenceEqual(expected.Result));
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 2);

            if (null != queryMock)
                queryMock.VerifyAll();
        }

        [Test]
        public async Task Funnel_WithActors_Success()
        {
            var client = new KeenClient(SettingsEnv);
            var timeframe = QueryRelativeTimeframe.ThisHour();

            IEnumerable<FunnelStep> funnelsteps = new[]
            {
                new FunnelStep
                {
                    EventCollection = FunnelColA,
                    ActorProperty = "id",
                    WithActors = true
                },
                new FunnelStep
                {
                    EventCollection = FunnelColB,
                    ActorProperty = "id"
                },
            };

            var expected = new FunnelResult
            {
                Actors = new[] { new[] { "sam", "dean" }, null },
                Steps = new[]
                {
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColA,
                        WithActors = true
                    },
                    new FunnelResultStep
                    {
                        EventCollection = FunnelColB,
                    },
                },
                Result = new[] { 3, 2 }
            };


            Mock<IQueries> queryMock = null;
            if (UseMocks)
            {
                queryMock = new Mock<IQueries>();
                queryMock.Setup(m => m.Funnel(
                        It.Is<IEnumerable<FunnelStep>>(f => f.Equals(funnelsteps)),
                        It.Is<IQueryTimeframe>(t => t == timeframe),
                        It.Is<string>(t => t == "")
                      ))
                  .Returns(Task.FromResult(expected));

                client.Queries = queryMock.Object;
            }

            var reply = (await client.QueryFunnelAsync(funnelsteps, timeframe));
            Assert.NotNull(reply);
            Assert.NotNull(reply.Actors);
            Assert.AreEqual(reply.Actors.Count(), 2);
            Assert.NotNull(reply.Result);
            Assert.True(reply.Result.SequenceEqual(expected.Result));
            Assert.NotNull(reply.Steps);
            Assert.AreEqual(reply.Steps.Count(), 2);

            if (null != queryMock)
                queryMock.VerifyAll();
        }
    }
}

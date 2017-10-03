using Keen.Core.Query;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;


namespace Keen.Core.Test
{
    /// <summary>
    /// Integration tests for Queries. These will exercise more than unit tests, like the
    /// integration between KeenClient, Queries and KeenHttpClient.
    /// </summary>
    class QueryTests_Integration : TestBase
    {
        [Test]
        public async Task QueryFilter_NotContains_Success()
        {
            var queriesUrl = HttpTests.GetUriForResource(SettingsEnv,
                                                         KeenConstants.QueriesResource);

            var handler = new FuncHandler()
            {
                PreProcess = (req, ct) =>
                {
                    var queryStr = req.RequestUri.Query;

                    // Make sure our filter properties are in the query string
                    Assert.IsTrue(queryStr.Contains("propertyName") &&
                                  queryStr.Contains("four") &&
                                  queryStr.Contains(QueryFilter.FilterOperator.NotContains()));
                },
                ProduceResultAsync = (req, ct) =>
                {
                    return HttpTests.CreateJsonStringResponseAsync(new { result = 2 });
                },
                DeferToDefault = false
            };

            // NOTE : This example shows use of UrlToMessageHandler, but since we only make one
            // request to a single endpoint, we could just directly use the FuncHandler here.
            var urlHandler = new UrlToMessageHandler(
                new Dictionary<Uri, IHttpMessageHandler>
                {
                    { queriesUrl, handler }
                })
            { DeferToDefault = false };

            var client = new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                   new HttpClientCache(),
                                                   null,
                                                   new DelegatingHandlerMock(urlHandler))
            });

            var filters = new List<QueryFilter>
            {
                new QueryFilter("propertyName", QueryFilter.FilterOperator.NotContains(), "four")
            };

            var count = await client.QueryAsync(
                QueryType.Count(),
                "testCollection",
                "",
                QueryRelativeTimeframe.ThisMonth(),
                filters);

            Assert.IsNotNull(count);
            Assert.AreEqual("2", count);
        }

        [Test]
        public async Task QueryFilter_NullPropertyValue_Success()
        {
            // TODO : Consolidate this FuncHandler/KeenClient setup into a helper method.

            var handler = new FuncHandler()
            {
                PreProcess = (req, ct) =>
                {
                    var queryStr = req.RequestUri.Query;

                    // Make sure our filter properties are in the query string
                    Assert.IsTrue(queryStr.Contains("propertyName") &&
                                  queryStr.Contains("null") &&
                                  queryStr.Contains(QueryFilter.FilterOperator.Equals()));
                },
                ProduceResultAsync = (req, ct) =>
                {
                    return HttpTests.CreateJsonStringResponseAsync(new { result = 2 });
                },
                DeferToDefault = false
            };

            var client = new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                   new HttpClientCache(),
                                                   null,
                                                   new DelegatingHandlerMock(handler))
            });

            var filters = new List<QueryFilter>
            {
                new QueryFilter("propertyName", QueryFilter.FilterOperator.Equals(), null)
            };

            var count = await client.QueryAsync(
                QueryType.Count(),
                "testCollection",
                "",
                QueryRelativeTimeframe.ThisMonth(),
                filters);

            Assert.IsNotNull(count);
            Assert.AreEqual("2", count);
        }

        [Test]
        public async Task Query_AvailableQueries_Success()
        {
            var queriesResource = HttpTests.GetUriForResource(SettingsEnv, KeenConstants.QueriesResource);

            var expectedQueries = new Dictionary<string, string>()
            {
                { "select_unique_url", $"{queriesResource.AbsolutePath}/select_unique"},
                { "minimum", $"{queriesResource.AbsolutePath}/minimum" },
                { "extraction_url", $"{queriesResource.AbsolutePath}/extraction" },
                { "percentile", $"{queriesResource.AbsolutePath}/percentile" },
                { "funnel_url", $"{queriesResource.AbsolutePath}/funnel" },
                { "average", $"{queriesResource.AbsolutePath}/average" },
                { "median", $"{queriesResource.AbsolutePath}/median" },
                { "maximum", $"{queriesResource.AbsolutePath}/maximum" },
                { "count_url", $"{queriesResource.AbsolutePath}/count" },
                { "count_unique_url", $"{queriesResource.AbsolutePath}/count_unique" },
                { "sum", $"{queriesResource.AbsolutePath}/sum"}
            };

            FuncHandler handler = new FuncHandler()
            {
                ProduceResultAsync = (request, ct) =>
                {
                    return HttpTests.CreateJsonStringResponseAsync(expectedQueries);
                }
            };

            var client = new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                          new HttpClientCache(),
                                                          null,
                                                          new DelegatingHandlerMock(handler))
            });

            var actualQueries = await client.GetQueries();

            Assert.AreEqual(expectedQueries.Count, actualQueries.Count());
            foreach (var expectedQuery in expectedQueries)
            {
                Assert.That(actualQueries.Contains(expectedQuery));
            }
        }

        [Test]
        public async Task Query_SimpleCount_Success()
        {
            string expectedResult = "10";
            string collection = "myEvents";
            QueryType analysis = QueryType.Count();

            var expectedResponse = new Dictionary<string, string>()
            {
                { "result", expectedResult},
            };

            FuncHandler handler = new FuncHandler()
            {
                ProduceResultAsync = (request, ct) =>
                {
                    var expectedUri = new Uri($"{HttpTests.GetUriForResource(SettingsEnv, KeenConstants.QueriesResource)}/" +
                                              $"{analysis}?event_collection={collection}");
                    Assert.AreEqual(expectedUri, request.RequestUri);
                    return HttpTests.CreateJsonStringResponseAsync(expectedResponse);
                }
            };

            var client = new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                          new HttpClientCache(),
                                                          null,
                                                          new DelegatingHandlerMock(handler))
            });

            var actualResult = await client.Queries.Metric(analysis, collection, null);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public async Task Query_SimpleAverage_Success()
        {
            string expectedResult = "10";
            string collection = "myEvents";
            QueryType analysis = QueryType.Average();
            string targetProperty = "someProperty";

            var expectedResponse = new Dictionary<string, string>()
            {
                { "result", expectedResult},
            };

            FuncHandler handler = new FuncHandler()
            {
                ProduceResultAsync = (request, ct) =>
                {
                    var expectedUri = new Uri($"{HttpTests.GetUriForResource(SettingsEnv, KeenConstants.QueriesResource)}/" +
                                              $"{analysis}?event_collection={collection}&target_property={targetProperty}");
                    Assert.AreEqual(expectedUri, request.RequestUri);
                    return HttpTests.CreateJsonStringResponseAsync(expectedResponse);
                }
            };

            var client = new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                          new HttpClientCache(),
                                                          null,
                                                          new DelegatingHandlerMock(handler))
            });

            var actualResult = await client.Queries.Metric(analysis, collection, targetProperty);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public async Task Query_SimpleCountGroupBy_Success()
        {
            var expectedResults = new List<string>() { "10", "20" };
            var expectedGroups = new List<string>() { "group1", "group2" };
            string collection = "myEvents";
            QueryType analysis = QueryType.Average();
            string targetProperty = "someProperty";
            string groupBy = "someGroupProperty";
            var expectedGroupResults = expectedResults.Zip(
                expectedGroups,
                (result, group) => new Dictionary<string, string>()
                {
                    { groupBy, group },
                    { "result", result }
                });

            var expectedResponse = new
            {
                result = expectedGroupResults
            };

            FuncHandler handler = new FuncHandler()
            {
                ProduceResultAsync = (request, ct) =>
                {
                    var expectedUri = new Uri($"{HttpTests.GetUriForResource(SettingsEnv, KeenConstants.QueriesResource)}/" +
                                              $"{analysis}?event_collection={collection}&target_property={targetProperty}&group_by={groupBy}");
                    Assert.AreEqual(expectedUri, request.RequestUri);
                    return HttpTests.CreateJsonStringResponseAsync(expectedResponse);
                }
            };

            var client = new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                          new HttpClientCache(),
                                                          null,
                                                          new DelegatingHandlerMock(handler))
            });

            var actualGroupResults = await client.Queries.Metric(analysis, collection, targetProperty, groupBy);

            Assert.AreEqual(expectedGroupResults.Count(), actualGroupResults.Count());
            foreach (var actualGroupResult in actualGroupResults)
            {
                var expectedGroupResult = expectedGroupResults.Where((result) => result[groupBy] == actualGroupResult.Group).First();
                Assert.AreEqual(expectedGroupResult["result"], actualGroupResult.Value);
            }
        }
    }
}

using Keen.Core.Query;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;


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

        class QueryParameters
        {
            internal string EventCollection = "myEvents";
            internal QueryType Analysis = QueryType.Count();
            internal string TargetProperty;
            internal String GroupBy;
            internal QueryRelativeTimeframe Timeframe;
            internal QueryInterval Interval;

            internal List<KeyValuePair<string, string>> GetQueryParameters()
            {
                var queryParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("event_collection", EventCollection),
                };

                if (null != TargetProperty) { queryParameters.Add(new KeyValuePair<string, string>("target_property", TargetProperty)); }
                if (null != GroupBy) { queryParameters.Add(new KeyValuePair<string, string>("group_by", GroupBy)); }
                if (null != Timeframe) { queryParameters.Add(new KeyValuePair<string, string>("timeframe", Timeframe.ToString())); }
                if (null != Interval) { queryParameters.Add(new KeyValuePair<string, string>("interval", Interval)); }

                return queryParameters;
            }
        }

        FuncHandler CreateQueryRequestHandler(QueryParameters queryParameters, object response)
        {
            var parameters = queryParameters.GetQueryParameters();
            StringBuilder queryStringBuilder = new StringBuilder();
            foreach (var parameter in parameters)
            {
                if (queryStringBuilder.Length != 0)
                {
                    queryStringBuilder.Append('&');
                }

                queryStringBuilder.Append($"{parameter.Key}={parameter.Value}");
            }

            return new FuncHandler()
            {
                ProduceResultAsync = (request, ct) =>
                {
                    var expectedUri = new Uri($"{HttpTests.GetUriForResource(SettingsEnv, KeenConstants.QueriesResource)}/" +
                                              $"{queryParameters.Analysis}?{queryStringBuilder}");
                    Assert.AreEqual(expectedUri, request.RequestUri);
                    return HttpTests.CreateJsonStringResponseAsync(response);
                }
            };
        }

        KeenClient CreateQueryTestKeenClient(QueryParameters queryParameters, object response)
        {
            var handler = CreateQueryRequestHandler(queryParameters, response);

            return new KeenClient(SettingsEnv, new TestKeenHttpClientProvider()
            {
                ProvideKeenHttpClient =
                    (url) => KeenHttpClientFactory.Create(url,
                                                          new HttpClientCache(),
                                                          null,
                                                          new DelegatingHandlerMock(handler))
            });
        }

        [Test]
        public async Task Query_SimpleCount_Success()
        {
            var queryParameters = new QueryParameters();

            string expectedResult = "10";

            var expectedResponse = new Dictionary<string, string>()
            {
                { "result", expectedResult},
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResult = await client.Queries.Metric(queryParameters.Analysis, queryParameters.EventCollection, null);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public async Task Query_SimpleAverage_Success()
        {
            var queryParameters = new QueryParameters()
            {
                Analysis = QueryType.Average(),
                TargetProperty = "someProperty"
            };

            string expectedResult = "10";

            var expectedResponse = new Dictionary<string, string>()
            {
                { "result", expectedResult},
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public async Task Query_SimpleCountGroupBy_Success()
        {
            var queryParameters = new QueryParameters()
            {
                TargetProperty = "someProperty",
                GroupBy = "someGroupProperty"
            };

            var expectedResults = new List<string>() { "10", "20" };
            var expectedGroups = new List<string>() { "group1", "group2" };
            var expectedGroupResults = expectedResults.Zip(
                expectedGroups,
                (result, group) => new Dictionary<string, string>()
                {
                    { queryParameters.GroupBy, group },
                    { "result", result }
                });

            var expectedResponse = new
            {
                result = expectedGroupResults
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.GroupBy);

            Assert.AreEqual(expectedGroupResults.Count(), actualResult.Count());
            foreach (var actualGroupResult in actualResult)
            {
                var expectedGroupResult = expectedGroupResults.Where((result) => result[queryParameters.GroupBy] == actualGroupResult.Group).First();
                Assert.AreEqual(expectedGroupResult["result"], actualGroupResult.Value);
            }
        }

        [Test]
        public async Task Query_SimpleCountInterval_Success()
        {
            var queryParameters = new QueryParameters()
            {
                TargetProperty = "someProperty",
                Timeframe = QueryRelativeTimeframe.ThisNHours(2),
                Interval = QueryInterval.EveryNHours(1)
            };

            var expectedCounts = new List<string>() { "10", "20" };
            var expectedTimeframes = new List<QueryAbsoluteTimeframe>()
            {
                new QueryAbsoluteTimeframe(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1)),
                new QueryAbsoluteTimeframe(DateTime.Now.AddHours(-1), DateTime.Now)
            };
            var expectedResults = expectedCounts.Zip(
                expectedTimeframes,
                (count, time) => new { timeframe = time, value = count });

            var expectedResponse = new
            {
                result = expectedResults
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.Timeframe,
                queryParameters.Interval);

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            var expectedEnumerator = expectedResults.GetEnumerator();
            var actualEnumerator = actualResults.GetEnumerator();
            expectedEnumerator.MoveNext();
            actualEnumerator.MoveNext();
            do
            {
                var expected = expectedEnumerator.Current;
                var actual = actualEnumerator.Current;
                Assert.AreEqual(expected.timeframe.Start, actual.Start);
                Assert.AreEqual(expected.timeframe.End, actual.End);
                Assert.AreEqual(expected.value, actual.Value);
            } while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext());
        }
    }
}

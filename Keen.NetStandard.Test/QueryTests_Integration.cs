using Keen.Core.Query;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

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

            internal virtual string GetResourceName() => Analysis;

            internal virtual List<KeyValuePair<string, string>> GetQueryParameters()
            {
                var queryParameters = new List<KeyValuePair<string, string>>();

                if (null != EventCollection) { queryParameters.Add(new KeyValuePair<string, string>(KeenConstants.QueryParmEventCollection, EventCollection)); }
                if (null != TargetProperty) { queryParameters.Add(new KeyValuePair<string, string>(KeenConstants.QueryParmTargetProperty, TargetProperty)); }
                if (null != GroupBy) { queryParameters.Add(new KeyValuePair<string, string>(KeenConstants.QueryParmGroupBy, GroupBy)); }
                if (null != Timeframe) { queryParameters.Add(new KeyValuePair<string, string>(KeenConstants.QueryParmTimeframe, Timeframe.ToString())); }
                if (null != Interval) { queryParameters.Add(new KeyValuePair<string, string>(KeenConstants.QueryParmInterval, Interval)); }

                return queryParameters;
            }
        }

        class ExtractionParameters : QueryParameters
        {
            internal ExtractionParameters()
            {
                Analysis = null;
            }

            internal override string GetResourceName() => KeenConstants.QueryExtraction;
        }

        class FunnelParameters : QueryParameters
        {
            internal IEnumerable<FunnelStep> Steps;

            internal FunnelParameters()
            {
                EventCollection = null;
                Analysis = null;
            }

            internal override string GetResourceName() => KeenConstants.QueryFunnel;

            internal override List<KeyValuePair<string, string>> GetQueryParameters()
            {
                var parameters = base.GetQueryParameters();
                parameters.Add(new KeyValuePair<string, string>(
                    KeenConstants.QueryParmSteps,
                    Uri.EscapeDataString(JArray.FromObject(Steps).ToString(Newtonsoft.Json.Formatting.None))));
                return parameters;
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
                                              $"{queryParameters.GetResourceName()}?{queryStringBuilder}");
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
        public async Task Query_SimpleSelectUnique_Success()
        {
            var queryParameters = new QueryParameters()
            {
                Analysis = QueryType.SelectUnique(),
                TargetProperty = "targetProperty"
            };

            string[] results =
            {
                "this",
                "that",
                "theOtherThing"
            };

            var expectedResponse = new 
            {
                result = results,
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty);

            string expectedResultString = string.Join(',', results);
            Assert.AreEqual(expectedResultString, actualResult);
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
                null,
                queryParameters.GroupBy);

            Assert.AreEqual(expectedGroupResults.Count(), actualResult.Count());
            foreach (var actualGroupResult in actualResult)
            {
                var expectedGroupResult = expectedGroupResults.Where((result) => result[queryParameters.GroupBy] == actualGroupResult.Group).First();
                Assert.AreEqual(expectedGroupResult["result"], actualGroupResult.Value);
            }
        }

        [Test]
        public async Task Query_SimpleSelectUniqueGroupBy_Success()
        {
            var queryParameters = new QueryParameters()
            {
                Analysis = QueryType.SelectUnique(),
                TargetProperty = "someProperty",
                GroupBy = "someGroupProperty"
            };

            var expectedResults = new List<string[]>() { new string[] { "10", "20" }, new string[] { "30", "40" } };
            var expectedGroups = new List<string>() { "group1", "group2" };
            var expectedGroupResults = expectedResults.Zip(
                expectedGroups,
                (result, group) => new
                {
                    someGroupProperty = group,
                    result = result
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
                var expectedGroupResult = expectedGroupResults.Where((result) => result.someGroupProperty == actualGroupResult.Group).First();
                Assert.AreEqual(string.Join(',', expectedGroupResult.result), actualGroupResult.Value);
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
            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                var expected = expectedEnumerator.Current;
                var actual = actualEnumerator.Current;
                Assert.AreEqual(expected.timeframe.Start, actual.Start);
                Assert.AreEqual(expected.timeframe.End, actual.End);
                Assert.AreEqual(expected.value, actual.Value);
            }
        }

        [Test]
        public async Task Query_SimpleSelectUniqueInterval_Success()
        {
            var queryParameters = new QueryParameters()
            {
                Analysis = QueryType.SelectUnique(),
                TargetProperty = "someProperty",
                Timeframe = QueryRelativeTimeframe.ThisNHours(2),
                Interval = QueryInterval.EveryNHours(1)
            };

            var expectedCounts = new List<string[]>() { new string[] { "10", "20" }, new string[] { "30", "40" } };
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
            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                var expected = expectedEnumerator.Current;
                var actual = actualEnumerator.Current;
                Assert.AreEqual(expected.timeframe.Start, actual.Start);
                Assert.AreEqual(expected.timeframe.End, actual.End);
                Assert.AreEqual(string.Join(',', expected.value), actual.Value);
            }
        }

        [Test]
        public async Task Query_SimpleCountGroupByInterval_Success()
        {
            var queryParameters = new QueryParameters()
            {
                TargetProperty = "someProperty",
                Timeframe = QueryRelativeTimeframe.ThisNHours(2),
                Interval = QueryInterval.EveryNHours(1),
                GroupBy = "someGroupProperty"
            };

            var expectedResults = new[]
            {
                new
                {
                    timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1)),
                    value = new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string>{ { queryParameters.GroupBy, "group1" }, { "result", "10" } },
                        new Dictionary<string, string>{ { queryParameters.GroupBy, "group2" }, { "result", "20" } },
                    }
                },
                new
                {
                    timeframe = new QueryAbsoluteTimeframe(DateTime.Now.AddHours(-1), DateTime.Now),
                    value = new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string>{ { queryParameters.GroupBy, "group1" }, { "result", "30" } },
                        new Dictionary<string, string>{ { queryParameters.GroupBy, "group2" }, { "result", "40" } },
                    }
                }
            };

            var expectedResponse = new
            {
                result = expectedResults
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.GroupBy,
                queryParameters.Timeframe,
                queryParameters.Interval);

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            var actualEnumerator = actualResults.GetEnumerator();
            foreach (var expected in expectedResults)
            {
                actualEnumerator.MoveNext();
                var actual = actualEnumerator.Current;
                // Validate the interval is correct
                Assert.AreEqual(expected.timeframe.Start, actual.Start);
                Assert.AreEqual(expected.timeframe.End, actual.End);

                // Validate the results for the group within the time interval
                Assert.AreEqual(expected.value.Count, actual.Value.Count());
                var actualGroupResultEnumerator = actual.Value.GetEnumerator();
                foreach (var expectedGroupResult in expected.value)
                {
                    actualGroupResultEnumerator.MoveNext();
                    var actualGroupResult = actualGroupResultEnumerator.Current;
                    Assert.AreEqual(expectedGroupResult[queryParameters.GroupBy], actualGroupResult.Group);
                    Assert.AreEqual(expectedGroupResult["result"], actualGroupResult.Value);
                }
            }
        }

        [Test]
        public async Task Query_SimpleSelectUniqueGroupByInterval_Success()
        {
            var queryParameters = new QueryParameters()
            {
                Analysis = QueryType.SelectUnique(),
                TargetProperty = "someProperty",
                Timeframe = QueryRelativeTimeframe.ThisNHours(2),
                Interval = QueryInterval.EveryNHours(1),
                GroupBy = "someGroupProperty"
            };

            string resultsJson = @"[
                {
                    ""timeframe"": {
                        ""start"": ""2017-10-14T00:00:00.000Z"",
                        ""end"": ""2017-10-15T00:00:00.000Z""
                    },
                    ""value"": [
                        {
                            ""someGroupProperty"": ""group1"",
                            ""result"": [
                                ""10"",
                                ""20""
                            ]
                        },
                        {
                            ""someGroupProperty"": ""group2"",
                            ""result"": [
                                ""30"",
                                ""40""
                            ]
                        }
                    ]
                },
                {
                    ""timeframe"": {
                        ""start"": ""2017-10-15T00:00:00.000Z"",
                        ""end"": ""2017-10-16T00:00:00.000Z""
                    },
                    ""value"": [
                        {
                            ""someGroupProperty"": ""group1"",
                            ""result"": [
                                ""50"",
                                ""60""
                            ]
                        },
                        {
                            ""someGroupProperty"": ""group2"",
                            ""result"": [
                                ""70"",
                                ""80""
                            ]
                        }
                    ]
                }
            ]";

            var expectedResults = JArray.Parse(resultsJson);

            var expectedResponse = new
            {
                result = expectedResults
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.GroupBy,
                queryParameters.Timeframe,
                queryParameters.Interval);

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            var actualEnumerator = actualResults.GetEnumerator();
            foreach (var expected in expectedResults)
            {
                actualEnumerator.MoveNext();
                var actual = actualEnumerator.Current;
                // Validate the interval is correct
                Assert.AreEqual(DateTime.Parse(expected["timeframe"]["start"].Value<string>()), actual.Start);
                Assert.AreEqual(DateTime.Parse(expected["timeframe"]["end"].Value<string>()), actual.End);

                // Validate the results for the group within the time interval
                Assert.AreEqual(expected["value"].Count(), actual.Value.Count());
                var actualGroupResultEnumerator = actual.Value.GetEnumerator();
                foreach (var expectedGroupResult in expected["value"])
                {
                    actualGroupResultEnumerator.MoveNext();
                    var actualGroupResult = actualGroupResultEnumerator.Current;
                    Assert.AreEqual(expectedGroupResult[queryParameters.GroupBy].Value<string>(), actualGroupResult.Group);
                    Assert.AreEqual(string.Join(',', expectedGroupResult["result"].Values<string>()), actualGroupResult.Value);
                }
            }
        }

        [Test]
        public async Task Query_SimpleExtraction_Success()
        {
            var queryParameters = new ExtractionParameters()
            {
                Timeframe = QueryRelativeTimeframe.ThisNHours(2),
            };

            string resultsJson = @"[
                {
                    ""keen"": {
                        ""created_at"": ""2012-07-30T21:21:46.566000+00:00"",
                        ""timestamp"": ""2012-07-30T21:21:46.566000+00:00"",
                        ""id"": """"
                    },
                    ""user"": {
                        ""email"": ""dan@keen.io"",
                        ""id"": ""4f4db6c7777d66ffff000000""
                    },
                    ""user_agent"": {
                        ""browser"": ""chrome"",
                        ""browser_version"": ""20.0.1132.57"",
                        ""platform"": ""macos""
                    }
                },
                {
                    ""keen"": {
                        ""created_at"": ""2012-07-30T21:40:05.386000+00:00"",
                        ""timestamp"": ""2012-07-30T21:40:05.386000+00:00"",
                        ""id"": """"
                    },
                    ""user"": {
                        ""email"": ""michelle@keen.io"",
                        ""id"": ""4fa2cccccf546ffff000006""
                    },
                    ""user_agent"": {
                        ""browser"": ""chrome"",
                        ""browser_version"": ""20.0.1132.57"",
                        ""platform"": ""macos""
                    }
                }
            ]";

            var expectedResults = JArray.Parse(resultsJson);

            var expectedResponse = new
            {
                result = expectedResults
            };

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.Extract(
                queryParameters.EventCollection,
                queryParameters.Timeframe);

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            var actualEnumerator = actualResults.GetEnumerator();
            foreach (var expected in expectedResults)
            {
                actualEnumerator.MoveNext();
                JToken actual = actualEnumerator.Current;
                // Validate the result is correct
                Assert.AreEqual(expected["user"]["email"].Value<string>(), actual["user"]["email"].Value<string>());
                Assert.AreEqual(expected["user"]["id"].Value<string>(), actual["user"]["id"].Value<string>());
                Assert.AreEqual(expected["user_agent"]["browser"].Value<string>(), actual["user_agent"]["browser"].Value<string>());
            }
        }

        [Test]
        public async Task Query_SimpleFunnel_Success()
        {
            var queryParameters = new FunnelParameters()
            {
                Steps = new FunnelStep[]
                {
                    new FunnelStep() {EventCollection = "signed up", ActorProperty = "visitor.guid", Timeframe = QueryRelativeTimeframe.ThisNDays(7)},
                    new FunnelStep() {EventCollection = "completed profile", ActorProperty = "user.guid", Timeframe = QueryRelativeTimeframe.ThisNDays(7)},
                    new FunnelStep() {EventCollection = "referred user", ActorProperty = "user.guid", Timeframe = QueryRelativeTimeframe.ThisNDays(7)},
                }
            };

            string responseJson = @"{
                ""result"": [
                    3,
                    1,
                    0
                ],
                ""steps"": [
                    {
                        ""actor_property"": ""visitor.guid"",
                        ""event_collection"": ""signed up"",
                        ""timeframe"": ""this_7_days""
                    },
                    {
                        ""actor_property"": ""user.guid"",
                        ""event_collection"": ""completed profile"",
                        ""timeframe"": ""this_7_days""
                    },
                    {
                        ""actor_property"": ""user.guid"",
                        ""event_collection"": ""referred user"",
                        ""timeframe"": ""this_7_days""
                    }
                ]
            }";

            var expectedResponse = JObject.Parse(responseJson);

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.Funnel(
                queryParameters.Steps);

            var expectedResults = expectedResponse["result"];

            Assert.AreEqual(expectedResults.Count(), actualResults.Result.Count());
            var actualEnumerator = actualResults.Result.GetEnumerator();
            foreach (var expected in expectedResults)
            {
                actualEnumerator.MoveNext();
                var actual = actualEnumerator.Current;
                // Validate the result is correct
                Assert.AreEqual(expected.Value<int>(), actual);
            }
        }
    }
}

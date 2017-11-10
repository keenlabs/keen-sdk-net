using Keen.Core.Query;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Collections.Specialized;

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

            Assert.That(actualQueries, Is.EquivalentTo(expectedQueries));
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

            internal virtual Dictionary<string, string> GetQueryParameters()
            {
                var queryParameters = new Dictionary<string, string>();

                if (null != EventCollection) { queryParameters[KeenConstants.QueryParmEventCollection] = EventCollection; }
                if (null != TargetProperty) { queryParameters[KeenConstants.QueryParmTargetProperty] = TargetProperty; }
                if (null != GroupBy) { queryParameters[KeenConstants.QueryParmGroupBy] = GroupBy; }
                if (null != Timeframe) { queryParameters[KeenConstants.QueryParmTimeframe] = Timeframe.ToString(); }
                if (null != Interval) { queryParameters[KeenConstants.QueryParmInterval] = Interval; }

                return queryParameters;
            }

            internal MultiAnalysisParam GetMultiAnalysisParameter(string label)
            {
                return new MultiAnalysisParam(
                    label,
                    String.IsNullOrEmpty(TargetProperty) ?
                        new MultiAnalysisParam.Metric(Analysis) :
                        new MultiAnalysisParam.Metric(Analysis, TargetProperty));
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

            internal override Dictionary<string, string> GetQueryParameters()
            {
                var parameters = base.GetQueryParameters();
                parameters[KeenConstants.QueryParmSteps] = JArray.FromObject(Steps).ToString(Newtonsoft.Json.Formatting.None);
                return parameters;
            }
        }

        class MultiAnalysisParameters : QueryParameters
        {
            internal IEnumerable<QueryParameters> Analyses;
            internal IList<string> Labels;

            internal MultiAnalysisParameters()
            {
                Analysis = null;
            }

            internal override string GetResourceName() => KeenConstants.QueryMultiAnalysis;

            internal override Dictionary<string, string> GetQueryParameters()
            {
                var parameters = base.GetQueryParameters();

                var multiAnalysisParameters = GetMultiAnalysisParameters();

                var jObjects = multiAnalysisParameters.Select(x =>
                    new JProperty(x.Label, JObject.FromObject(
                        string.IsNullOrEmpty(x.TargetProperty) ?
                            (object)new { analysis_type = x.Analysis } :
                            new { analysis_type = x.Analysis, target_property = x.TargetProperty })));

                var analysesJson = JsonConvert.SerializeObject(
                    new JObject(jObjects),
                    Formatting.None,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                parameters[KeenConstants.QueryParmAnalyses] = analysesJson;

                return parameters;
            }

            internal IEnumerable<MultiAnalysisParam> GetMultiAnalysisParameters()
            {
                return Analyses.Zip(Labels, (parameters, label) => parameters.GetMultiAnalysisParameter(label));
            }
        }

        FuncHandler CreateQueryRequestHandler(QueryParameters queryParameters, object response)
        {
            // Create a NameValueCollection with all expected query string parameters
            var expectedQueryStringCollection = new NameValueCollection();
            foreach (var parameter in queryParameters.GetQueryParameters())
            {
                if (null != parameter.Value)
                {
                    expectedQueryStringCollection[parameter.Key] = parameter.Value;
                }
            }

            return new FuncHandler()
            {
                ProduceResultAsync = (request, ct) =>
                {
                    var expectedPath =
                        $"{HttpTests.GetUriForResource(SettingsEnv, KeenConstants.QueriesResource)}/" +
                        $"{queryParameters.GetResourceName()}";

                    string actualPath = 
                        $"{request.RequestUri.Scheme}{Uri.SchemeDelimiter}" +
                        $"{request.RequestUri.Authority}{request.RequestUri.AbsolutePath}";

                    Assert.AreEqual(expectedPath, actualPath);

                    var actualQueryStringCollection = HttpUtility.ParseQueryString(request.RequestUri.Query);

                    Assert.That(actualQueryStringCollection, Is.EquivalentTo(expectedQueryStringCollection));

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

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                null);

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

            var expectedSdkResult = expectedGroupResults.Select((result) =>
            {
                return new QueryGroupValue<string>(
                    result["result"],
                    result[queryParameters.GroupBy]);
            });

            Assert.That(actualResult, Is.EquivalentTo(expectedSdkResult));
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

            var expectedSdkResult = expectedGroupResults.Select((result) =>
            {
                return new QueryGroupValue<string>(
                    string.Join(',', result.result),
                    result.someGroupProperty);
            });

            Assert.That(actualResult, Is.EquivalentTo(expectedSdkResult));
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

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.Timeframe,
                queryParameters.Interval);

            var expectedSdkResult = expectedResults.Select((result) =>
            {
                return new QueryIntervalValue<string>(
                    string.Join(',', result.value),
                    result.timeframe.Start,
                    result.timeframe.End);
            });

            Assert.That(actualResult, Is.EquivalentTo(expectedSdkResult));
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

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.Timeframe,
                queryParameters.Interval);

            var expectedSdkResult = expectedResults.Select((result) =>
            {
                return new QueryIntervalValue<string>(
                    string.Join(',', result.value),
                    result.timeframe.Start,
                    result.timeframe.End);
            });

            Assert.That(actualResult, Is.EquivalentTo(expectedSdkResult));
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

            var actualResult = (await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.GroupBy,
                queryParameters.Timeframe,
                queryParameters.Interval));

            var expectedSdkResult = expectedResults.Select((intervals) =>
            {
                return new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>(
                    intervals.value.Select((group) => new QueryGroupValue<string>(
                        group["result"],
                        group[queryParameters.GroupBy])),
                    intervals.timeframe.Start,
                    intervals.timeframe.End
                    );
            });

            // Use JArray objects as a way to normalize types here, since the 
            // concrete types won't match for the QueryInternalValue IEnumerable implementation.
            Assert.That(JArray.FromObject(actualResult), Is.EquivalentTo(JArray.FromObject(expectedSdkResult)));
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

            var actualResult = await client.Queries.Metric(
                queryParameters.Analysis,
                queryParameters.EventCollection,
                queryParameters.TargetProperty,
                queryParameters.GroupBy,
                queryParameters.Timeframe,
                queryParameters.Interval);

            var expectedSdkResult = expectedResults.Select((intervalToken) =>
            {
                return new QueryIntervalValue<IEnumerable<QueryGroupValue<string>>>(
                    intervalToken["value"].Select((groupToken) =>
                    {
                        return new QueryGroupValue<string>(
                                string.Join(',', groupToken["result"]),
                                groupToken[queryParameters.GroupBy].Value<string>());
                    }),
                    intervalToken["timeframe"]["start"].Value<DateTime>(),
                    intervalToken["timeframe"]["end"].Value<DateTime>());
            });

            // Use JArray objects as a way to normalize types here, since the 
            // concrete types won't match for the QueryInternalValue IEnumerable implementation.
            Assert.That(JArray.FromObject(actualResult), Is.EquivalentTo(JArray.FromObject(expectedSdkResult)));
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

            Assert.That(actualResults, Is.EquivalentTo(expectedResults));
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

            var expectedResults = expectedResponse["result"].Values<int>();

            Assert.That(actualResults.Result, Is.EquivalentTo(expectedResults));

            var expectedSteps = queryParameters.Steps.Select((step) => new FunnelResultStep() { EventCollection = step.EventCollection, ActorProperty = step.ActorProperty, Timeframe = step.Timeframe });

            Assert.That(actualResults.Steps, Is.EquivalentTo(expectedSteps));
        }

        [Test]
        public async Task Query_SimpleMultiAnalysis_Success()
        {
            var queryParameters = new MultiAnalysisParameters()
            {
                Labels = new string[]
                {
                    "first analysis",
                    "second analysis"
                },
                Analyses = new QueryParameters[]
                {
                    new QueryParameters(),
                    new QueryParameters()
                    {
                        Analysis = QueryType.Average(),
                        TargetProperty = "targetProperty"
                    }
                }
            };

            string responseJson = $"{{\"result\":{{ \"{queryParameters.Labels[0]}\" : 12345, \"{queryParameters.Labels[1]}\" : 54321 }} }}";

            var expectedResponse = JObject.Parse(responseJson);

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.MultiAnalysis(
                queryParameters.EventCollection,
                queryParameters.GetMultiAnalysisParameters(),
                timeframe: null,
                filters: null,
                timezone: null);

            var expectedResults = expectedResponse["result"];
            var transformedResults = JObject.FromObject(actualResults.ToDictionary((pair) => pair.Key, (pair) => int.Parse(pair.Value)));

            Assert.That(transformedResults, Is.EquivalentTo(expectedResults));
        }

        [Test]
        public async Task Query_SimpleMultiAnalysisGroupBy_Success()
        {
            var queryParameters = new MultiAnalysisParameters()
            {
                Labels = new string[]
                {
                    "first analysis",
                    "second analysis"
                },
                Analyses = new QueryParameters[]
                {
                    new QueryParameters(),
                    new QueryParameters()
                    {
                        Analysis = QueryType.Average(),
                        TargetProperty = "targetProperty"
                    }
                },
                GroupBy = "groupByProperty"
            };

            string responseJson = $"{{\"result\":[" +
                $"{{\"{queryParameters.GroupBy}\":\"group1\",\"{queryParameters.Labels[0]}\":12345,\"{queryParameters.Labels[1]}\":54321}}," +
                $"{{\"{queryParameters.GroupBy}\":\"group2\",\"{queryParameters.Labels[0]}\":67890,\"{queryParameters.Labels[1]}\":9876}}" +
                $"]}}";

            var expectedResponse = JObject.Parse(responseJson);

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.MultiAnalysis(
                queryParameters.EventCollection,
                queryParameters.GetMultiAnalysisParameters(),
                null,
                null,
                queryParameters.GroupBy,
                null);

            var expectedResults = expectedResponse["result"];

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            foreach (var group in new string[] { "group1", "group2" })
            {
                var actualGroupResult = actualResults.Where((result) => result.Group == group).First();
                var expectedGroupResult = expectedResults.Where((result) => result[queryParameters.GroupBy].Value<string>() == group).First();
                foreach (var label in queryParameters.Labels)
                {
                    // Validate the result is correct
                    Assert.AreEqual(expectedGroupResult[label].Value<int>(), int.Parse(actualGroupResult.Value[label]));
                }
            }
        }

        [Test]
        public async Task Query_SimpleMultiAnalysisInterval_Success()
        {
            var queryParameters = new MultiAnalysisParameters()
            {
                Labels = new string[]
                {
                    "first analysis",
                    "second analysis"
                },
                Analyses = new QueryParameters[]
                {
                    new QueryParameters(),
                    new QueryParameters()
                    {
                        Analysis = QueryType.Average(),
                        TargetProperty = "targetProperty"
                    }
                },
                Interval = QueryInterval.Daily()
            };

            string responseJson = "{\"result\":[" +
                    "{\"timeframe\":{\"start\":\"2017-10-14T00:00:00.000Z\",\"end\":\"2017-10-15T00:00:00.000Z\"}," +
                    "\"value\":{" +
                        $"\"{queryParameters.Labels[0]}\":12345,\"{queryParameters.Labels[1]}\":54321}}" +
                    "}," +
                    "{\"timeframe\":{\"start\":\"2017-10-15T00:00:00.000Z\",\"end\":\"2017-10-16T00:00:00.000Z\"}," +
                    "\"value\":{" +
                        $"\"{queryParameters.Labels[0]}\":123,\"{queryParameters.Labels[1]}\":321}}" +
                    "}" +
                "]}";

            var expectedResponse = JObject.Parse(responseJson);

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.MultiAnalysis(
                queryParameters.EventCollection,
                queryParameters.GetMultiAnalysisParameters(),
                timeframe: null,
                interval: queryParameters.Interval,
                filters: null,
                timezone: null);

            var expectedResults = expectedResponse["result"];

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            var results = expectedResults.Zip(actualResults, (expected, actual) => new { Expected = expected, Actual = actual });
            foreach (var result in results)
            {
                Assert.AreEqual(DateTime.Parse(result.Expected["timeframe"]["start"].Value<string>()), result.Actual.Start);
                Assert.AreEqual(DateTime.Parse(result.Expected["timeframe"]["end"].Value<string>()), result.Actual.End);

                foreach (var label in queryParameters.Labels)
                {
                    // Validate the result is correct
                    Assert.AreEqual(result.Expected["value"][label].Value<int>(), int.Parse(result.Actual.Value[label]));
                }
            }
        }

        [Test]
        public async Task Query_SimpleMultiAnalysisIntervalGroupBy_Success()
        {
            var queryParameters = new MultiAnalysisParameters()
            {
                Labels = new string[]
                {
                    "first analysis",
                    "second analysis"
                },
                Analyses = new QueryParameters[]
                {
                    new QueryParameters(),
                    new QueryParameters()
                    {
                        Analysis = QueryType.Average(),
                        TargetProperty = "targetProperty"
                    }
                },
                GroupBy = "groupByProperty",
                Interval = QueryInterval.Daily()
            };

            string responseJson = "{\"result\":[" +
                "{\"timeframe\":{\"start\":\"2017-10-14T00:00:00.000Z\",\"end\":\"2017-10-15T00:00:00.000Z\"}," +
                "\"value\":[" +
                    $"{{\"{queryParameters.GroupBy}\":\"group1\",\"{queryParameters.Labels[0]}\":12345,\"{queryParameters.Labels[1]}\":54321}}," +
                    $"{{\"{queryParameters.GroupBy}\":\"group2\",\"{queryParameters.Labels[0]}\":67890,\"{queryParameters.Labels[1]}\":9876}}" +
                "]}," +
                "{\"timeframe\":{\"start\":\"2017-10-15T00:00:00.000Z\",\"end\":\"2017-10-16T00:00:00.000Z\"}," +
                "\"value\":[" +
                    $"{{\"{queryParameters.GroupBy}\":\"group1\",\"{queryParameters.Labels[0]}\":123,\"{queryParameters.Labels[1]}\":321}}," +
                    $"{{\"{queryParameters.GroupBy}\":\"group2\",\"{queryParameters.Labels[0]}\":456,\"{queryParameters.Labels[1]}\":654}}" +
                "]}" +
            "]}";

            var expectedResponse = JObject.Parse(responseJson);

            var client = CreateQueryTestKeenClient(queryParameters, expectedResponse);

            var actualResults = await client.Queries.MultiAnalysis(
                queryParameters.EventCollection,
                queryParameters.GetMultiAnalysisParameters(),
                groupby: queryParameters.GroupBy,
                interval: queryParameters.Interval);

            var expectedResults = expectedResponse["result"];

            Assert.AreEqual(expectedResults.Count(), actualResults.Count());
            var results = expectedResults.Zip(actualResults, (expected, actual) => new { Expected = expected, Actual = actual });
            foreach (var result in results)
            {
                Assert.AreEqual(DateTime.Parse(result.Expected["timeframe"]["start"].Value<string>()), result.Actual.Start);
                Assert.AreEqual(DateTime.Parse(result.Expected["timeframe"]["end"].Value<string>()), result.Actual.End);

                foreach (var group in new string[] { "group1", "group2" })
                {
                    var expectedGroupResult = result.Expected["value"].Where((groupResult) => groupResult[queryParameters.GroupBy].Value<string>() == group).First();
                    var actualGroupResult = result.Actual.Value.Where((groupResult) => groupResult.Group == group).First();

                    foreach (var label in queryParameters.Labels)
                    {
                        Assert.AreEqual(expectedGroupResult[label].Value<int>(), int.Parse(actualGroupResult.Value[label]));
                    }
                }
            }
        }
    }
}

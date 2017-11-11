using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Keen.Core;
using Keen.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;


namespace Keen.Test
{
    [TestFixture]
    internal class HttpTests : TestBase
    {
        [Test]
        public void GetSdkVersion_Success()
        {
            string sdkVersion = KeenUtil.GetSdkVersion();

            Assert.IsNotNull(sdkVersion);
            Assert.IsNotEmpty(sdkVersion);
            Assert.IsTrue(sdkVersion.StartsWith(".net"));
        }

        [Test]
        public async Task DefaultHeaders_Success()
        {
            object responseData = new[] { new { result = 2 } };

            var handler = new FuncHandler()
            {
                PreProcess = (req, ct) =>
                {
                    // Make sure the default headers are in place
                    Assert.IsTrue(req.Headers.Contains("Keen-Sdk"));
                    Assert.AreEqual(KeenUtil.GetSdkVersion(), req.Headers.GetValues("Keen-Sdk").Single());

                    Assert.IsTrue(req.Headers.Contains("Authorization"));

                    var key = req.Headers.GetValues("Authorization").Single();
                    Assert.IsTrue(SettingsEnv.ReadKey == key ||
                                  SettingsEnv.WriteKey == key ||
                                  SettingsEnv.MasterKey == key);
                },
                ProduceResultAsync = (req, ct) =>
                {
                    return CreateJsonStringResponseAsync(responseData);
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

            // Try out all the endpoints
            Assert.DoesNotThrow(() => client.GetSchemas());

            // Remaining operations expect an object, not an array of objects
            responseData = new { result = 2 };

            var @event = new { AProperty = "AValue" };
            Assert.DoesNotThrow(() => client.AddEvent("AddEventTest", @event));
            Assert.DoesNotThrow(() => client.AddEvents("AddEventTest", new[] { @event }));

            Assert.DoesNotThrow(() => client.DeleteCollection("DeleteColTest"));
            Assert.IsNotNull(client.GetSchema("AddEventTest"));

            // Currently all the queries/extraction go through the same KeenWebApiRequest() call.
            var count = await client.QueryAsync(
                QueryType.Count(),
                "testCollection",
                "",
                QueryRelativeTimeframe.ThisMonth());

            Assert.IsNotNull(count);
            Assert.AreEqual("2", count);
        }

        internal static Task<HttpResponseMessage> CreateJsonStringResponseAsync(object data)
        {
            return HttpTests.CreateJsonStringResponseAsync(data, HttpStatusCode.OK);
        }

        internal static Task<HttpResponseMessage> CreateJsonStringResponseAsync(
            object data,
            HttpStatusCode statusCode)
        {
            HttpResponseMessage mockResponse = new HttpResponseMessage(statusCode);
            var dataStr = (data is Array ? JArray.FromObject(data).ToString(Formatting.None) :
                                           JObject.FromObject(data).ToString(Formatting.None));
            var content = new StringContent(dataStr);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            mockResponse.Content = content;

            return Task.FromResult(mockResponse);
        }

        // For communicating errors in mock responses.
        internal static Task<HttpResponseMessage> CreateJsonStringResponseAsync(
            HttpStatusCode statusCode,
            string message,
            string errorCode)
        {
            var content = new StringContent(JObject.FromObject(
                // Matches what the server returns and KeenUtil.CheckApiErrorCode() expects.
                new
                {
                    message = message,
                    error_code = errorCode
                }).ToString(Formatting.None));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage mockResponse = new HttpResponseMessage(statusCode)
            {
                Content = content
            };

            return Task.FromResult(mockResponse);
        }

        internal static Uri GetUriForResource(IProjectSettings projectSettings, string resource)
        {
            string keenUrl = projectSettings.KeenUrl;
            string projectId = projectSettings.ProjectId;

            return new Uri($"{keenUrl}projects/{projectId}/{resource}");
        }

        internal static Task ValidateRequest(HttpRequestMessage request, string expectedRequestBody)
        {
            return HttpTests.ValidateRequest(request, JObject.Parse(expectedRequestBody));
        }

        internal static async Task ValidateRequest(HttpRequestMessage request, JObject expectedRequestJson)
        {
            // Request should have a body
            Assert.NotNull(request.Content);

            string requestBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            var requestJson = JObject.Parse(requestBody);

            Assert.IsTrue(JToken.DeepEquals(expectedRequestJson, requestJson));
        }
    }
}

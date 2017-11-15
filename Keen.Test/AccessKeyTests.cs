using System;
using System.Collections.Generic;
using System.Linq;
using Keen.AccessKey;
using Keen.Core;
using Keen.Query;
using Newtonsoft.Json.Linq;
using NUnit.Framework;


namespace Keen.Test
{
    [TestFixture]
    class AccessKeyTests : TestBase
    {
        private IProjectSettings _settings = null;
        private KeenClient _client = null;

        [SetUp]
        public void AccessKeyTestsSetup()
        {
            _settings = new ProjectSettingsProvider(projectId: "X",
                                                    masterKey: SettingsEnv.MasterKey);
            _client = new KeenClient(_settings);
        }

        [Test]
        public void CreateAccessKey_Success()
        {
            // TODO : Replace AccessKeysMock with Moq as per PR feedback.
            if (UseMocks)
                _client.AccessKeys = new AccessKeysMock(_settings,
                    createAccessKey: new Func<AccessKeyDefinition, IProjectSettings, JObject>((e, p) =>
                    {
                        Assert.True(p == _settings, "Incorrect Settings");
                        Assert.NotNull(e.Name, "Expected a name for the newly created Key");
                        Assert.NotNull(e.Permitted, "Expected a list of high level actions this key can perform");
                        Assert.NotNull(e.Options, "Expected an object containing more details about the key’s permitted and restricted functionality");
                        if ((p == _settings) && (e.Name == "TestAccessKey") && (e.IsActive) && e.Permitted.First() == "queries" && e.Options.CachedQueries.Allowed.First() == "my_cached_query")
                            return new JObject();
                        else
                            throw new Exception("Unexpected value");
                    }));

            var permitted = new HashSet<string>() { "queries" };

            var filters = new List<QueryFilter>()
            {
                new QueryFilter("customer.id", QueryFilter.FilterOperator.Equals(), "asdf12345z")
            };

            var cachedQueries = new CachedQueries
            {
                Allowed = new HashSet<string>() { "my_cached_query" }
            };

            var options = new Options()
            {
                Queries = new AccessKey.Queries { Filters = filters },
                CachedQueries = cachedQueries
            };

            var accessKey = new AccessKeyDefinition
            {
                Name = "TestAccessKey",
                IsActive = true,
                Permitted = permitted,
                Options = options
            };

            Assert.DoesNotThrow(() => _client.CreateAccessKey(accessKey));
        }

        [Test]
        public void CreateAccessKey_With_All_Properties_Given_As_Null_Success()
        {
            // TODO : Replace AccessKeysMock with Moq as per PR feedback.
            if (UseMocks)
                _client.AccessKeys = new AccessKeysMock(_settings,
                    createAccessKey: new Func<AccessKeyDefinition, IProjectSettings, JObject>((e, p) =>
                    {
                        Assert.True(p == _settings, "Incorrect Settings");
                        Assert.NotNull(e.Name, "Expected a name for the newly created Key");
                        Assert.NotNull(e.Permitted, "Expected a list of high level actions this key can perform");
                        Assert.NotNull(e.Options, "Expected an object containing more details about the key’s permitted and restricted functionality");
                        if ((p == _settings) && (e.Name == "TestAccessKey") && (e.IsActive) && e.Permitted.First() == "queries")
                            return new JObject();
                        else
                            throw new Exception("Unexpected value");
                    }));

            var permitted = new HashSet<string>() { "queries" };

            // TODO : Can't null just be the default when we construct these? We should look at
            // some factories or a builder mechanism to configure these correctly, or at least add
            // more validation code that will throw before sending with reasons for why it's
            // malformed. For example, why set a SavedQueries instance if "saved_queries" isn't
            // permitted? We could help devs consuming the SDK catch that early.

            var cachedQueries = new CachedQueries() { Allowed = null, Blocked = null };

            var savedQuaries = new SavedQueries()
            {
                Allowed = null,
                Blocked = null,
                Filters = null
            };

            var datasets = new Datasets() { Allowed = null, Blocked = null, Operations = null };
            var writes = new Writes() { Autofill = null };
            
            var options = new Options()
            {
                Queries = new AccessKey.Queries { Filters = null },
                CachedQueries = cachedQueries,
                SavedQueries = savedQuaries,
                Datasets = datasets,
                Writes = writes
            };

            // TODO : We need to more carefully test out what is required and what isn't. For
            // example, I thought the 'options' member could be omitted, e.g. with 'schema'
            // permitted.

            var accessKey = new AccessKeyDefinition
            {
                Name = "TestAccessKey",
                IsActive = true,
                Permitted = permitted,
                Options = options
            };

            Assert.DoesNotThrow(() => _client.CreateAccessKey(accessKey));
        }
    }
}

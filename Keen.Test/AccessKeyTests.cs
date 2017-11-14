using Keen.Core;
using Keen.AccessKey;
using Keen.Query;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Test
{
    [TestFixture]
    class AccessKeyTests : TestBase
    {
        [Test]
        public void CreateAccessKey_Success()
        {
            var settings = new ProjectSettingsProvider(projectId: "X", masterKey: SettingsEnv.MasterKey); // Replace X with respective value
            var client = new KeenClient(settings);

            if (UseMocks)
                client.AccessKeys = new AccessKeysMock(settings,
                    createAccessKey: new Func<AccessKey.AccessKey, IProjectSettings, JObject>((e, p) =>
                    {
                        Assert.True(p == settings, "Incorrect Settings");
                        Assert.NotNull(e.Name, "Expected a name for the newly created Key");
                        Assert.NotNull(e.Permitted, "Expected a list of high level actions this key can perform");
                        Assert.NotNull(e.Options, "Expected an object containing more details about the key’s permitted and restricted functionality");
                        if ((p == settings) && (e.Name == "TestAccessKey") && (e.IsActive) && e.Permitted.First() == "queries" && e.Options.CachedQueries.Allowed.First() == "my_cached_query")
                            return new JObject();
                        else
                            throw new Exception("Unexpected value");
                    }));

            HashSet<string> permissions = new HashSet<string>() { "queries" };
            List<QueryFilter> qFilters = new List<QueryFilter>() { new QueryFilter("customer.id", QueryFilter.FilterOperator.Equals(), "asdf12345z") };
            CachedQueries cachedQueries = new CachedQueries();
            cachedQueries.Allowed = new HashSet<string>() { "my_cached_query" };
            Options options = new Options()
            {
                Queries = new AccessKey.Queries { Filters = qFilters },
                CachedQueries = cachedQueries
            };

            Assert.DoesNotThrow(() => client.CreateAccessKey(new AccessKey.AccessKey { Name = "TestAccessKey", IsActive = true, Options = options, Permitted = permissions }));
        }


        [Test]
        public void CreateAccessKey_With_All_Properties_Given_As_Null_Success()
        {
            var settings = new ProjectSettingsProvider(projectId: "X", masterKey: SettingsEnv.MasterKey); // Replace X with respective value
            var client = new KeenClient(settings);

            if (UseMocks)
                client.AccessKeys = new AccessKeysMock(settings,
                    createAccessKey: new Func<AccessKey.AccessKey, IProjectSettings, JObject>((e, p) =>
                    {
                        Assert.True(p == settings, "Incorrect Settings");
                        Assert.NotNull(e.Name, "Expected a name for the newly created Key");
                        Assert.NotNull(e.Permitted, "Expected a list of high level actions this key can perform");
                        Assert.NotNull(e.Options, "Expected an object containing more details about the key’s permitted and restricted functionality");
                        if ((p == settings) && (e.Name == "TestAccessKey") && (e.IsActive) && e.Permitted.First() == "queries" && e.Options.CachedQueries.Allowed.First() == "my_cached_query")
                            return new JObject();
                        else
                            throw new Exception("Unexpected value");
                    }));

            HashSet<string> permissions = new HashSet<string>() { "queries" };
            List<QueryFilter> qFilters = new List<QueryFilter>() { new QueryFilter("customer.id", QueryFilter.FilterOperator.Equals(), "asdf12345z") };
            CachedQueries cachedQueries = new CachedQueries() { Allowed = null, Blocked = null };
            SavedQueries savedQuaries = new SavedQueries() { Allowed = null, Blocked = null, Filters = null };
            Datasets datasets = new Datasets() { Allowed = null, Blocked = null, Operations = null };
            Writes writes = new Writes() { Autofill = null };
            cachedQueries.Allowed = new HashSet<string>() { "my_cached_query" };
            Options options = new Options()
            {
                Queries = new AccessKey.Queries { Filters = qFilters },
                CachedQueries = cachedQueries,
                SavedQueries = savedQuaries,
                Datasets = datasets,
                Writes = writes
            };

            Assert.DoesNotThrow(() => client.CreateAccessKey(new AccessKey.AccessKey { Name = "TestAccessKey", IsActive = true, Options = options, Permitted = permissions }));
        }

    }
}

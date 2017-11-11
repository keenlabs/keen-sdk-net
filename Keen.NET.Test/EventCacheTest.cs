using Keen.Core;
using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Keen.Net.Test
{
    [TestFixture]
    public class EventCacheTest : TestBase
    {
        static readonly object[] Providers = 
        {
            new object[] { new EventCacheMemory() },
            new object[] { EventCachePortable.New() }
        };

        [Test]
        [TestCaseSource("Providers")]
        public void AddEvent_Null_Throws(IEventCache cache)
        {
            Assert.ThrowsAsync<Keen.Core.KeenException>(() => cache.Add(null));
        }

        [Test]
        [TestCaseSource("Providers")]
        public async Task AddEvent_ValidObject_Success(IEventCache cache)
        {
            await cache.Add(new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
        }

        [Test]
        [TestCaseSource("Providers")]
        public async Task AddEvent_AddNotEmpty_Success(IEventCache cache)
        {
            await cache.Clear();
            Assert.Null(await cache.TryTake());
            await cache.Add(new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            Assert.NotNull(await cache.TryTake());
        }

        [Test]
        [TestCaseSource("Providers")]
        public async Task AddEvent_AddClearEmpty_Success(IEventCache cache)
        {
            await cache.Add( new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            await cache.Clear();
            Assert.Null(await cache.TryTake());
        }

        [Test]
        [TestCaseSource("Providers")]
        public async Task AddEvent_Iterate_Success(IEventCache cache)
        {
            await cache.Clear();
            await cache.Add( new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            await cache.Add( new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            Assert.NotNull(await cache.TryTake());
            Assert.NotNull(await cache.TryTake());
            Assert.Null(await cache.TryTake());
        }

        [Test]
        [TestCaseSource("Providers")]
        public void CachingPCL_SendEmptyEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(SettingsEnv, cache);
            Assert.DoesNotThrow(client.SendCachedEvents);
        }

        [Test]
        [TestCaseSource("Providers")]
        public void CachingPCL_ClearEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(SettingsEnv, cache);
            Assert.DoesNotThrow(() => client.EventCache.Clear());
        }

        [Test]
        [TestCaseSource("Providers")]
        public void CachingPCL_AddEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(SettingsEnv, cache);

            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { AProperty = "AValue" }));
            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { AProperty = "AValue" }));
            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { AProperty = "AValue" }));
        }

        [Test]
        [TestCaseSource("Providers")]
        public async Task CachingPCL_SendEventsParallel_Success(IEventCache cache)
        {
            await cache.Clear();
            var client = new KeenClient(SettingsEnv, cache);
            if (UseMocks)
                client.Event = new EventMock(SettingsEnv,
                    addEvents: (e, p) => new List<CachedEvent>());

            (from i in Enumerable.Range(1,100)
            select new { AProperty = "AValue" })
            .AsParallel()
            .ForAll(e=>client.AddEvent("CachedEventTest", e));

            await client.SendCachedEventsAsync();
            Assert.Null(await client.EventCache.TryTake(), "Cache is empty");
        }
    
    }
}

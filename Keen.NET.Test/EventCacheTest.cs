using Keen.Core;
using Keen.Net;
using Keen.Core.EventCache;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Keen.Net.Test
{
    [TestFixture]
    public class EventCacheTest : TestBase
    {
        static object[] Providers = 
        {
            new object[] { new EventCacheMemory() },
            new object[] { EventCachePortable.New() },
        };

        [Test]
        [TestCaseSource("Providers")]
        [ExpectedException("Keen.Core.KeenException")]
        public async Task AddEvent_Null_Throws(IEventCache cache)
        {
            await cache.Add(null);
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
        public async void AddEvent_Iterate_Success(IEventCache cache)
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
            var client = new KeenClient(settingsEnv, cache);
            Assert.DoesNotThrow(() => client.SendCachedEvents());
        }

        [Test]
        [TestCaseSource("Providers")]
        public void CachingPCL_ClearEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(settingsEnv, cache);
            Assert.DoesNotThrow(() => client.EventCache.Clear());
        }

        [Test]
        [TestCaseSource("Providers")]
        public void CachingPCL_AddEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(settingsEnv, cache);

            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { AProperty = "AValue" }));
            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { AProperty = "AValue" }));
            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { AProperty = "AValue" }));
        }

        [Test]
        [TestCaseSource("Providers")]
        public async void CachingPCL_SendEventsParallel_Success(IEventCache cache)
        {
            await cache.Clear();
            var client = new KeenClient(settingsEnv, cache);
            if (UseMocks)
                client.Event = new EventMock(settingsEnv,
                    AddEvents: new Func<JObject, IProjectSettings, IEnumerable<CachedEvent>>((e, p) =>
                    {
                        return new List<CachedEvent>();
                    }));

            (from i in Enumerable.Range(1,100)
            select new { AProperty = "AValue" })
            .AsParallel()
            .ForAll((e)=>client.AddEvent("CachedEventTest", e));

            await client.SendCachedEventsAsync();
            Assert.Null(await client.EventCache.TryTake(), "Cache is empty");
        }
    
    }
}

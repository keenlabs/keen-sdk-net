using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using NUnit.Framework;


namespace Keen.Core.Test
{
    [TestFixture]
    public class EventCacheTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            DeleteFileCache();
        }

        [TearDown]
        public override void TearDown()
        {
            DeleteFileCache();
        }

        void DeleteFileCache()
        {
            var cachePath = EventCachePortable.GetKeenFolderPath();
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, recursive: true);
            }
        }

        static readonly object[] Providers =
        {
            new object[] { new EventCacheMemory() },
            new object[] { EventCachePortable.InstanceAsync.Value.Result }
        };

        [Test]
        [TestCaseSource(nameof(Providers))]
        public void AddEvent_Null_Throws(IEventCache cache)
        {
            Assert.ThrowsAsync<KeenException>(() => cache.AddAsync(null));
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public async Task AddEvent_ValidObject_Success(IEventCache cache)
        {
            await cache.AddAsync(new CachedEvent("url", JObject.FromObject(new { Property = "Value" })));
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public async Task AddEvent_AddNotEmpty_Success(IEventCache cache)
        {
            await cache.ClearAsync();
            Assert.Null(await cache.TryTakeAsync());
            await cache.AddAsync(new CachedEvent("url", JObject.FromObject(new { Property = "Value" })));
            Assert.NotNull(await cache.TryTakeAsync());
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public async Task AddEvent_AddClearEmpty_Success(IEventCache cache)
        {
            await cache.AddAsync(new CachedEvent("url", JObject.FromObject(new { Property = "Value" })));
            await cache.ClearAsync();
            Assert.Null(await cache.TryTakeAsync());
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public async Task AddEvent_Iterate_Success(IEventCache cache)
        {
            await cache.ClearAsync();
            await cache.AddAsync(new CachedEvent("url", JObject.FromObject(new { Property = "Value" })));
            await cache.AddAsync(new CachedEvent("url", JObject.FromObject(new { Property = "Value" })));
            Assert.NotNull(await cache.TryTakeAsync());
            Assert.NotNull(await cache.TryTakeAsync());
            Assert.Null(await cache.TryTakeAsync());
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public void CachingPCL_SendEmptyEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(SettingsEnv, cache);
            Assert.DoesNotThrow(client.SendCachedEvents);
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public void CachingPCL_ClearEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(SettingsEnv, cache);
            Assert.DoesNotThrow(() => client.EventCache.ClearAsync());
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public void CachingPCL_AddEvents_Success(IEventCache cache)
        {
            var client = new KeenClient(SettingsEnv, cache);

            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { Property = "Value" }));
            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { Property = "Value" }));
            Assert.DoesNotThrow(() => client.AddEvent("CachedEventTest", new { Property = "Value" }));
        }

        [Test]
        [TestCaseSource(nameof(Providers))]
        public async Task CachingPCL_SendEventsParallel_Success(IEventCache cache)
        {
            await cache.ClearAsync();
            var client = new KeenClient(SettingsEnv, cache);
            if (UseMocks)
                client.Event = new EventMock(SettingsEnv,
                                             addEvents: (e, p) => new List<CachedEvent>());

            (from i in Enumerable.Range(1, 100)
             select new { Property = "Value" })
                .AsParallel()
                .ForAll(e => client.AddEvent("CachedEventTest", e));

            await client.SendCachedEventsAsync();
            Assert.Null(await client.EventCache.TryTakeAsync(), "Cache is empty");
        }

        [Test]
        public async Task DurableCache_EventsAreSavedAndRestored()
        {
            // Create a test event to add to the cache
            var testEvent = new CachedEvent("CollectionName", JObject.FromObject(new { Property = "Value" }));

            // Create the cache to be tested
            var cache = await EventCachePortableTestable.NewTestableAsync();

            // Add the event to the cache
            await cache.AddAsync(testEvent);

            // Destroy the cache object and clear the static event queue
            cache.ResetStaticMembers();
            cache = null;

            // The event should have been written to disk, and creating a new cache should populate
            // from disk
            var newCache = await EventCachePortableTestable.NewTestableAsync();

            var actualEvent = await newCache.TryTakeAsync();

            // Event read should be equal to the original
            Assert.NotNull(actualEvent);
            Assert.AreEqual(testEvent.Event, actualEvent.Event);
            Assert.AreEqual(testEvent.Collection, actualEvent.Collection);
            Assert.AreEqual(testEvent.Error, actualEvent.Error);

            // Shouldn't be more events stored
            Assert.Null(await newCache.TryTakeAsync());
        }
    }
}

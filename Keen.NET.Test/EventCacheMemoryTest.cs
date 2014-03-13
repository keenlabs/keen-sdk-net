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

namespace Keen.NET.Test
{
    [TestFixture]
    public class EventCacheMemoryTest
    {
        [Test]
        public void AddEvent_Null_Throws()
        {
            IEventCache cache = new EventCacheMemory();
            Assert.Throws<KeenException>(() => cache.Add(null));
        }

        [Test]
        public void AddEvent_ValidObject_Success()
        {
            IEventCache cache = new EventCacheMemory();
            Assert.DoesNotThrow(() => cache.Add(new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" }))));
        }

        [Test]
        public void AddEvent_AddNotEmpty_Success()
        {
            IEventCache cache = new EventCacheMemory();
            Assert.Null(cache.TryTake());
            cache.Add(new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            Assert.NotNull(cache.TryTake());
        }

        [Test]
        public void AddEvent_AddClearEmpty_Success()
        {
            IEventCache cache = new EventCacheMemory();
            cache.Add( new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            Assert.DoesNotThrow(()=> cache.Clear());
            Assert.Null(cache.TryTake());
        }

        [Test]
        public void AddEvent_Iterate_Success()
        {
            IEventCache cache = new EventCacheMemory();
            cache.Add( new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            cache.Add( new CachedEvent("url", JObject.FromObject( new { AProperty = "AValue" })));
            Assert.NotNull(cache.TryTake());
            Assert.NotNull(cache.TryTake());
            Assert.Null(cache.TryTake());
        }
    }
}

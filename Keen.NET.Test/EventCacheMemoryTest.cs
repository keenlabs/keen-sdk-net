using Keen.Core;
using Keen.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Assert.DoesNotThrow(() => cache.Add(new { AProperty = "AValue" }));
        }

        [Test]
        public void AddEvent_AddNotEmpty_Success()
        {
            IEventCache cache = new EventCacheMemory();
            Assert.True(cache.IsEmpty());
            cache.Add(new { AProperty = "AValue" });
            Assert.False(cache.IsEmpty());
        }

        [Test]
        public void AddEvent_AddClearEmpty_Success()
        {
            IEventCache cache = new EventCacheMemory();
            cache.Add(new { AProperty = "AValue" });
            Assert.DoesNotThrow(()=> cache.Clear());
            Assert.True(cache.IsEmpty());
        }

        [Test]
        public void AddEvent_Iterate_Success()
        {
            IEventCache cache = new EventCacheMemory();
            cache.Add(new { AProperty = "AValue" });
            cache.Add(new { AProperty = "AValue" });
            Assert.True(cache.Events().Count() == 2);
        }

    }
}

using System.Collections.Generic;
using System.Linq;

namespace Keen.NET_35
{
    /// <summary>
    /// <para>This is a simple memory-based cache provider. It has no cache-expiration policy.
    /// To use, pass an instance of this class when constructing KeenClient</para>
    /// <seealso cref="Keen.Core.KeenClient"/>
    /// </summary>
    public class EventCacheMemory : IEventCache
    {
        private readonly Queue<CachedEvent> events = new Queue<CachedEvent>();

        public void Add(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");

            lock (events)
                events.Enqueue(e);
        }

        public void Clear()
        {
            lock (events)
                events.Clear();
        }

        public CachedEvent TryTake()
        {
            lock (events)
                return events.Any() ? events.Dequeue() : null;
        }
    }
}

using Keen.Core;
using Keen.Core.EventCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Net
{
    public class EventCacheMemory : IEventCache
    {
        private Queue<CachedEvent> events = new Queue<CachedEvent>();

        public void Add(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");

            lock(events)
                events.Enqueue(e);
        }

        public void Clear()
        {
            lock(events)
                events.Clear();
        }

        public CachedEvent TryTake()
        {
            lock(events)
                return events.Any() ? events.Dequeue() : null;
        }
    }
}

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
        private List<CachedEvent> events = new List<CachedEvent>();

        public void Add(CachedEvent e)
        {
            if (null == e)
                throw new KeenException("Cached events may not be null");
            events.Add(e);
        }

        public void Clear()
        {
            events.Clear();
        }

        public bool IsEmpty()
        {
            return !events.Any();
        }

        public IEnumerable<CachedEvent> Events()
        {
            return events;
        }
    }
}

using Keen.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Net
{
    public class EventCacheMemory : IEventCache
    {
        private List<object> events = new List<object>();

        public void Add(object e)
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

        public IEnumerable<object> Events()
        {
            return events;
        }
    }
}

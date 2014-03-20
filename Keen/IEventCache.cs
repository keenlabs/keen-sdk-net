using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core.EventCache
{
    public class CachedEvent
    {
        public string Collection { get; set; }
        public JObject Event { get; set; }
        public Exception Error { get; set; }

        public CachedEvent(string collection, JObject e)
        {
            Collection = collection;
            Event = e;
        }
    }

    public interface IEventCache
    {
        void Add(CachedEvent e);
        CachedEvent TryTake();
        void Clear();
    }
}

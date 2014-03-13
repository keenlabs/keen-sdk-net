using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core.EventCache
{
    public class CachedEvent
    {
        public string Url { get; set; }
        public JObject Event { get; set; }
        public Exception Error { get; set; }

        public CachedEvent(string url, JObject e)
        {
            Url = url;
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

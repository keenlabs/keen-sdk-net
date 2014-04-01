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

        public CachedEvent(string collection, JObject e, Exception err = null)
        {
            Collection = collection;
            Event = e;
            Error = err;
        }

        public override string ToString()
        {
            return string.Format("CachedEvent:{{\n\"Collection\": \"{0}\",\n\"Event\":{1},\n\"Error\":\"{2}:{3}\"\n}}",
                Collection, Event, Error == null ? "null" : Error.GetType().Name, Error == null ? "" : Error.Message);
        }
    }

    public interface IEventCache
    {
        Task Add(CachedEvent e);
        Task<CachedEvent> TryTake();
        Task Clear();
    }
}

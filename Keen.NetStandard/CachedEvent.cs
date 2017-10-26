using Newtonsoft.Json.Linq;
using System;


namespace Keen.Core.EventCache
{
    /// <summary>
    /// CachedEvent is a container for user event data which associates the
    /// target event collection name and, if an error occurs during submission,
    /// the exception instance.
    /// </summary>
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
}

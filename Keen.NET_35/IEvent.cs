using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Keen.NET_35
{
    public interface IEvent
    {
        /// <summary>
        /// Return schema information for all the event collections in this project.
        /// </summary>
        /// <returns></returns>
        JArray GetSchemas();

        /// <summary>
        /// Insert multiple events in one or more collections in a single request.
        /// </summary>
        /// <param name="events"></param>
        /// <returns>Enumerable containing any rejected events</returns>
        IEnumerable<CachedEvent> AddEvents(JObject events);
    }
}

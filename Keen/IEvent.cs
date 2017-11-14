using System.Collections.Generic;
using System.Threading.Tasks;
using Keen.EventCache;
using Newtonsoft.Json.Linq;


namespace Keen.Core
{
    public interface IEvent
    {
        /// <summary>
        /// Return schema information for all the event collections in this project.
        /// </summary>
        /// <returns></returns>
        Task<JArray> GetSchemas();

        /// <summary>
        /// Insert multiple events in one or more collections in a single request.
        /// </summary>
        /// <param name="events"></param>
        /// <returns>Enumerable containing any rejected events</returns>
        Task<IEnumerable<CachedEvent>> AddEvents(JObject events);
    }
}

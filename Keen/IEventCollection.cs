using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace Keen.Core
{
    public interface IEventCollection
    {
        /// <summary>
        /// Returns schema information for this event collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        Task<JObject> GetSchema(string collection);

        /// <summary>
        /// Delete the entire event collection.
        /// </summary>
        /// <param name="collection">Name of collection</param>
        Task DeleteCollection(string collection);

        /// <summary>
        /// Insert one event at a time in a single request.
        /// </summary>
        /// <param name="collection">Name of collection</param>
        /// <param name="anEvent">Event data to insert</param>
        /// <returns></returns>
        Task AddEvent(string collection, JObject anEvent);
    }
}

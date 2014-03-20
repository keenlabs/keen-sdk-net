using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core
{
    public interface IEventCollection
    {
        Task<JObject> GetSchema(string collection);
        Task DeleteCollection(string collection);
        Task AddEvent(string collection, JObject anEvent);
    }
}

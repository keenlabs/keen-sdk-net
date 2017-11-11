using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Keen.Core.AccessKey
{
    /// <summary>
    /// Public interface for Access Key related functionalities 
    /// </summary>
    public interface IAccessKeys
    {
        /// <summary>
        /// Creates an Access Key
        /// </summary>
        /// <param name="accesskey"></param>
        /// <returns></returns>
        Task<JObject> CreateAccessKey(AccessKey accesskey);
    }
}

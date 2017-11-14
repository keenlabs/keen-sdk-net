using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace Keen.AccessKey
{
    /// <summary>
    /// Public interface for Access Key related functionalities 
    /// </summary>
    public interface IAccessKeys
    {
        // TODO : Flesh out public comments as per PR feedback.

        /// <summary>
        /// Creates an Access Key
        /// </summary>
        /// <param name="accesskey"></param>
        /// <returns></returns>
        Task<JObject> CreateAccessKey(AccessKeyDefinition accesskey);
    }
}

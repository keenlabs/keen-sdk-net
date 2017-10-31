using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core.AccessKey
{
    public interface IAccessKeys
    {
        Task<JObject> CreateAccessKey(AccessKey accesskey);
    }
}

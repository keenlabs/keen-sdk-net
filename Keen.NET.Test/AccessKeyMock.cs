using Keen.Core.AccessKey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Keen.Core;

namespace Keen.Net.Test
{
    /// <summary>
    /// AccessKeyMock provides an implementation of IAccessKeys with a constructor that 
    /// accepts delegates for each of the interface methods.
    /// The purpose of this is to allow test methods to set up a customized
    /// IAccessKeys for each test.
    /// </summary>
    class AccessKeysMock : IAccessKeys
    {
        private readonly IProjectSettings _settings;
        private readonly Func<AccessKey, IProjectSettings, JObject> _createAccessKey;

        public AccessKeysMock(IProjectSettings projSettings,
             Func<AccessKey, IProjectSettings, JObject> createAccessKey = null)
        {
            _settings = projSettings;
            _createAccessKey = createAccessKey ?? ((p, k) => new JObject());
        }

        public Task<JObject> CreateAccessKey(AccessKey accesskey)
        {
            return Task.Run(() => _createAccessKey(accesskey, _settings));
        }
    }
}

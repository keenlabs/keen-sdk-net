using System;
using System.Threading.Tasks;
using Keen.AccessKey;
using Keen.Core;
using Newtonsoft.Json.Linq;


namespace Keen.Test
{
    /// <summary>
    /// AccessKeyMock provides an implementation of IAccessKeys with a constructor that 
    /// accepts delegates for each of the interface methods.
    /// The purpose of this is to allow test methods to set up a customized
    /// IAccessKeys for each test.
    /// </summary>
    class AccessKeysMock : IAccessKeys
    {
        // TODO : Replace AccessKeysMock with Moq as per PR feedback.

        private readonly IProjectSettings _settings;
        private readonly Func<AccessKeyDefinition, IProjectSettings, JObject> _createAccessKey;

        public AccessKeysMock(IProjectSettings projSettings,
             Func<AccessKeyDefinition, IProjectSettings, JObject> createAccessKey = null)
        {
            _settings = projSettings;
            _createAccessKey = createAccessKey ?? ((p, k) => new JObject());
        }

        public Task<JObject> CreateAccessKey(AccessKeyDefinition accesskey)
        {
            return Task.Run(() => _createAccessKey(accesskey, _settings));
        }
    }
}

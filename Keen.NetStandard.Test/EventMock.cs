using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Keen.Core.Test
{
    /// <summary>
    /// EventMock provides an implementation of IEvent with a constructor that 
    /// accepts delegates for each of the interface methods.
    /// The purpose of this is to allow test methods to set up a customized
    /// IEvent for each test.
    /// </summary>
    class EventMock : IEvent
    {
        private readonly IProjectSettings _settings;
        private readonly Func<IProjectSettings, JArray> _getSchemas;
        private readonly Func<JObject, IProjectSettings, IEnumerable<CachedEvent>> _addEvents;

        public Task<JArray> GetSchemas()
        {
            return Task.Run(() => _getSchemas(_settings));
        }

        public Task<IEnumerable<CachedEvent>> AddEvents(JObject events)
        {
            return Task.Run(() => _addEvents(events, _settings));
        }

        public EventMock(IProjectSettings prjSettings,
            Func<IProjectSettings, JArray> getSchemas = null,
            Func<JObject, IProjectSettings, IEnumerable<CachedEvent>> addEvents = null)
        {
            _settings = prjSettings;
            _getSchemas = getSchemas ?? ((p) => new JArray());
            _addEvents = addEvents ?? ((p, e) => new List<CachedEvent>());
        }
    }
}

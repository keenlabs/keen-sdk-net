using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Keen.NET_35.Test
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

        public JArray GetSchemas()
        {
            return _getSchemas(_settings);
        }

        public IEnumerable<CachedEvent> AddEvents(JObject events)
        {
            return _addEvents(events, _settings);
        }

        public EventMock(IProjectSettings prjSettings,
            Func<IProjectSettings, JArray> getSchemas = null,
            Func<JObject, IProjectSettings, IEnumerable<CachedEvent>> addEvents = null)
        {
            _settings = prjSettings;
            _getSchemas = getSchemas ?? (p => new JArray());
            _addEvents = addEvents ?? ((p, e) => new List<CachedEvent>());
        }
    }
}

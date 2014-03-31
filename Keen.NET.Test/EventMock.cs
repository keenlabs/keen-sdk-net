using Keen.Core.EventCache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core
{
    /// <summary>
    /// EventMock provides an implementation of IEvent with a constructor that 
    /// accepts delegates for each of the interface methods.
    /// The purpose of this is to allow test methods to set up a customized
    /// IEvent for each test.
    /// </summary>
    class EventMock : IEvent
    {
        private IProjectSettings _settings;
        private Func<IProjectSettings, JObject> _getSchemas;
        private Func<JObject, IProjectSettings, IEnumerable<CachedEvent>> _addEvents;

        public Task<JObject> GetSchemas()
        {
            return Task.Run(() => _getSchemas(_settings));
        }

        public Task<IEnumerable<CachedEvent>> AddEvents(JObject events)
        {
            return Task.Run(() => _addEvents(events, _settings));
        }

        public EventMock(IProjectSettings prjSettings,
            Func<IProjectSettings, JObject> GetSchemas = null,
            Func<JObject, IProjectSettings, IEnumerable<CachedEvent>> AddEvents = null)
        {
            _settings = prjSettings;
            _getSchemas = GetSchemas ?? new Func<IProjectSettings, JObject>((p) => { return new JObject(); });
            _addEvents = AddEvents ?? new Func<JObject, IProjectSettings, IEnumerable<CachedEvent>>((p, e) => { return new List<CachedEvent>(); });
        }
    }
}

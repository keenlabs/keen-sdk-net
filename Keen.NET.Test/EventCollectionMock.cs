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
    /// EventCollectionMock provides an implementation of IEventCollection with a
    /// constructor that accepts delegates for each of the interface methods.
    /// The purpose of this is to allow test methods to set up a customized
    /// IEventCollection for each test.
    /// </summary>
    class EventCollectionMock : IEventCollection
    {
        private IProjectSettings _settings;
        private Func<string, IProjectSettings, JObject> _getSchema;
        private Action<string, IProjectSettings> _deleteCollection;
        private Action<string, JObject, IProjectSettings> _addEvent;

        public System.Threading.Tasks.Task<JObject> GetSchema(string collection)
        {
            return Task.Run(()=>_getSchema(collection, _settings));
        }

        public System.Threading.Tasks.Task DeleteCollection(string collection)
        {
            return Task.Run(() => _deleteCollection(collection, _settings));
        }

        public System.Threading.Tasks.Task AddEvent(string collection, JObject anEvent)
        {
            return Task.Run(() => _addEvent(collection, anEvent, _settings));
        }

        public EventCollectionMock(IProjectSettings prjSettings,
            Func<string, IProjectSettings, JObject> GetSchema = null,
            Action<string, IProjectSettings> DeleteCollection = null,
            Action<string, JObject, IProjectSettings> AddEvent = null)
        {
            _settings = prjSettings;
            _getSchema = GetSchema ?? new Func<string, IProjectSettings, JObject>((s, p) => { return new JObject(); });
            _deleteCollection = DeleteCollection ?? new Action<string, IProjectSettings>((s, p) => { });
            _addEvent = AddEvent ?? new Action<string, JObject, IProjectSettings>((s, p, e) => { });
        }
    }
}

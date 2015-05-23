using System;
using System.Threading.Tasks;
using Keen.Core;
using Newtonsoft.Json.Linq;

namespace Keen.Net.Test
{
    /// <summary>
    /// EventCollectionMock provides an implementation of IEventCollection with a
    /// constructor that accepts delegates for each of the interface methods.
    /// The purpose of this is to allow test methods to set up a customized
    /// IEventCollection for each test.
    /// </summary>
    class EventCollectionMock : IEventCollection
    {
        private readonly IProjectSettings _settings;
        private readonly Func<string, IProjectSettings, JObject> _getSchema;
        private readonly Action<string, IProjectSettings> _deleteCollection;
        private readonly Action<string, JObject, IProjectSettings> _addEvent;

        public Task<JObject> GetSchema(string collection)
        {
            return Task.Run(()=>_getSchema(collection, _settings));
        }

        public Task DeleteCollection(string collection)
        {
            return Task.Run(() => _deleteCollection(collection, _settings));
        }

        public Task AddEvent(string collection, JObject anEvent)
        {
            return Task.Run(() => _addEvent(collection, anEvent, _settings));
        }

        public EventCollectionMock(IProjectSettings prjSettings,
            Func<string, IProjectSettings, JObject> getSchema = null,
            Action<string, IProjectSettings> deleteCollection = null,
            Action<string, JObject, IProjectSettings> addEvent = null)
        {
            _settings = prjSettings;
            _getSchema = getSchema ?? ((s, p) => new JObject());
            _deleteCollection = deleteCollection ?? ((s, p) => { });
            _addEvent = addEvent ?? ((s, p, e) => { });
        }
    }
}

using System.IO;
using System.Threading.Tasks;

namespace Keen.NetStandard.Test
{
    class EventCachePortableTestable : EventCachePortable
    {
        public static async Task<EventCachePortableTestable> NewTestableAsync()
        {
            var instance = new EventCachePortableTestable();

            await instance.Initialize();

            return instance;
        }

        internal void ResetStaticMembers() => events.Clear();

        // TODO: Can GetKeenFolderPath just be made internal and internals made visible to this assembly?
        internal static string GetKeenFolderPathTestable() => GetKeenFolderPath();
    }
}
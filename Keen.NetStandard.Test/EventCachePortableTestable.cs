using System.IO;
using System.Threading.Tasks;

namespace Keen.Core.Test
{
    class EventCachePortableTestable : EventCachePortable
    {
        internal static async Task<EventCachePortableTestable> NewTestableAsync()
        {
            var instance = new EventCachePortableTestable();

            await instance.InitializeAsync();

            return instance;
        }

        internal void ResetStaticMembers() => events.Clear();
    }
}
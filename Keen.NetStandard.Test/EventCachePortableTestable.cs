using System.Threading.Tasks;
using Keen.Core;


namespace Keen.Test
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

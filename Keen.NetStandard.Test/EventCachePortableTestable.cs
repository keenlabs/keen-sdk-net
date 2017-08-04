

using System.Threading.Tasks;

namespace Keen.NetStandard.Tests
{
    class EventCachePortableTestable : EventCachePortable
    {
        public static async Task<EventCachePortableTestable> NewTestableAsync()
        {
            var instance = new EventCachePortableTestable();

            await instance.Initialize();

            return instance;
        }

        internal void ResetStaticMembers()
        {
            events.Clear();
        }
    }
}
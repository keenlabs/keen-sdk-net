using System.Threading.Tasks;


namespace Keen.Core.EventCache
{
    public interface IEventCache
    {
        Task Add(CachedEvent e);
        Task<CachedEvent> TryTake();
        Task Clear();
    }
}

using System.Threading.Tasks;


namespace Keen.NetStandard.EventCache
{
    public interface IEventCache
    {
        Task Add(CachedEvent e);
        Task<CachedEvent> TryTake();
        Task Clear();
    }
}

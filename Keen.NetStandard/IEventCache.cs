using System.Threading.Tasks;


namespace Keen.Core.EventCache
{
    public interface IEventCache
    {
        Task AddAsync(CachedEvent e);
        Task<CachedEvent> TryTakeAsync();
        Task ClearAsync();
    }
}

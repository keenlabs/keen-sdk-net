namespace Keen.NET_35
{
    public interface IEventCache
    {
        void Add(CachedEvent e);
        CachedEvent TryTake();
        void Clear();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core
{
    public interface IEventCache
    {
        void Add(object e);
        void Clear();
        bool IsEmpty();
        IEnumerable<object> Events();
    }
}

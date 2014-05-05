using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core.Query
{
    public sealed class QueryGroupValue<T>
    {
        public T Value { get; private set; }
        public string Group { get; private set; }

        public QueryGroupValue(T value, string group)
        {
            Value = value;
            Group = group;
        }
    }
}

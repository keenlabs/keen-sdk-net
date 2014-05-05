using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core.Query
{
    public sealed class QueryIntervalValue<T>
    {
        public T Value { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public QueryIntervalValue(T value, DateTime start, DateTime end)
        {
            Value = value;
            Start = start;
            End = end;
        }
    }
}

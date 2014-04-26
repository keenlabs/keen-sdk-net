using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core.Query
{
    public sealed class QueryIntervalCount
    {
        public int Value { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public QueryIntervalCount(int value, DateTime start, DateTime end)
        {
            Value = value;
            Start = start;
            End = end;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Keen.Core.Query
{
    /// <summary>
    /// Represents a set of values from a query performed with an interval parameter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class QueryIntervalValue<T>
    {
        /// <summary>
        /// The value for this interval. Varies with the type of query performed.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Start time for this interval.
        /// </summary>
        public DateTime Start { get; private set; }

        /// <summary>
        /// End time for this interval.
        /// </summary>
        public DateTime End { get; private set; }

        public QueryIntervalValue(T value, DateTime start, DateTime end)
        {
            Value = value;
            Start = start;
            End = end;
        }

        public override bool Equals(object obj)
        {
            var value = obj as QueryIntervalValue<T>;
            return value != null &&
                   value.Value.Equals(Value) &&
                   Start == value.Start &&
                   End == value.End;
        }

        public override int GetHashCode()
        {
            var hashCode = -1158026325;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + End.GetHashCode();
            return hashCode;
        }
    }
}

using System;


namespace Keen.NetStandard.Query
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
    }
}

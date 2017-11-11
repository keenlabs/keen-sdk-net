
namespace Keen.Query
{
    public sealed class QueryType
    {
        private readonly string _value;
        private QueryType(string value) { _value = value; }
        private QueryType(string value, int n) { _value = string.Format(value, n); }
        public override string ToString() { return _value; }
        public static implicit operator string(QueryType value) { return value.ToString(); }

        private static QueryType count = new QueryType("count");
        /// <summary>
        /// Returns the number of resources in the event collection. Parameter targetProperty is ignored.
        /// </summary>
        public static QueryType Count() { return count; }

        private static QueryType countunique = new QueryType("count_unique");
        /// <summary>
        /// Returns the number of unique resources in the event collection.
        /// </summary>
        public static QueryType CountUnique() { return countunique; }

        private static QueryType minimum = new QueryType("minimum");
        /// <summary>
        /// Returns the minimum value for the target property in the event collection.
        /// </summary>
        public static QueryType Minimum() { return minimum; }

        private static QueryType maximum = new QueryType("maximum");
        /// <summary>
        /// Returns the maximum value for the target property in the event collection.
        /// </summary>
        public static QueryType Maximum() { return maximum; }

        private static QueryType average = new QueryType("average");
        /// <summary>
        /// Returns the average across all numeric values for the target property.
        /// </summary>
        public static QueryType Average() { return average; }

        private static QueryType sum = new QueryType("sum");
        /// <summary>
        /// Returns the sum of all numeric resources in the event collection.
        /// </summary>
        public static QueryType Sum() { return sum; }

        private static QueryType selectunique = new QueryType("select_unique");
        /// <summary>
        /// Returns a list of unique resources in the event collection.
        /// </summary>
        public static QueryType SelectUnique() { return selectunique; }
    }
}

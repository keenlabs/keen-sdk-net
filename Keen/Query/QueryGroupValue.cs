using System.Collections.Generic;


namespace Keen.Query
{
    /// <summary>
    /// Represents the values from a query performed with a groupby. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class QueryGroupValue<T>
    {
        /// <summary>
        /// The value for the group. Varies with the type of query performed.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// The value of the groupby field for this value.
        /// </summary>
        public string Group { get; private set; }

        public QueryGroupValue(T value, string group)
        {
            Value = value;
            Group = group;
        }

        public override bool Equals(object obj)
        {
            var value = obj as QueryGroupValue<T>;
            return value != null &&
                   EqualityComparer<T>.Default.Equals(Value, value.Value) &&
                   Group == value.Group;
        }

        public override int GetHashCode()
        {
            var hashCode = -1845644250;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Group);
            return hashCode;
        }
    }
}

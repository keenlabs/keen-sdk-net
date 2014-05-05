using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core.Query
{
    /// <summary>
    /// Represents a filter that can be applied to a query.
    /// Because not all filter operators make sense for the different property data types, only certain operators are valid for each data type.
    /// </summary>
    public sealed class QueryFilter
    {
        public class OperatorConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(FilterOperator));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                // Don't need deserialization
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue( value.ToString());
            }
        }

        [JsonConverter(typeof(OperatorConverter))]
        public sealed class FilterOperator
        {
            private readonly string _value;
            private FilterOperator(string value) { _value = value; }
            public override string ToString() { return _value; }
            public static implicit operator string(FilterOperator value) { return value.ToString(); }
            /// <summary>
            /// Equal to.
            /// <para>Use with string, number, boolean</para>
            /// </summary>
            public static FilterOperator Equals() { return new FilterOperator("eq"); }


            /// <summary>
            /// Not equal to.
            /// <para>Use with string, number</para>
            /// </summary>
            public static FilterOperator NotEqual() { return new FilterOperator("ne"); }

            /// <summary>
            /// Less than.
            /// <para>Use with string, number</para>
            /// </summary>
            public static FilterOperator LessThan() { return new FilterOperator("lt"); }

            /// <summary>
            /// Less than or equal to.
            /// <para>Use with number</para>
            /// </summary>
            public static FilterOperator LessThanOrEqual() { return new FilterOperator("lte"); }

            /// <summary>
            /// Greater than.
            /// <para>Use with string, number</para>
            /// </summary>
            public static FilterOperator GreaterThan() { return new FilterOperator("gt"); }

            /// <summary>
            /// Greater than or equal to.
            /// <para>Use with number</para>
            /// </summary>
            public static FilterOperator GreaterThanOrEqual() { return new FilterOperator("gte"); }

            /// <summary>
            /// Whether a specific property exists on an event record.
            /// The Value property must be set to "true" or "false"
            /// <para>Use with string, number, boolean</para>
            /// </summary>
            public static FilterOperator Exists() { return new FilterOperator("exists"); }

            /// <summary>
            /// Whether the property value is in a give set of values.
            /// The Value property must be a JSON array of values, e.g.: "[1,2,4,5]"
            /// <para>Use with string, number, boolean</para>
            /// </summary>
            public static FilterOperator In() { return new FilterOperator("in"); }

            /// <summary>
            /// Whether the property value contains the give set of characters.
            /// <para>Use with strings</para>
            /// </summary>
            public static FilterOperator Contains() { return new FilterOperator("contains"); }

            /// <summary>
            /// Used to select events within a certain radius of the provided geo coordinate.
            /// <para>Use with geo analysis</para>
            /// </summary>
            public static FilterOperator Within() { return new FilterOperator("within"); }

        }


        public class GeoValue
        {
            [JsonProperty(PropertyName = "coordinates")]
            public double[] Coordinates { get; private set; }

            [JsonProperty(PropertyName = "max_distance_miles")]
            public double MaxDistanceMiles { get; private set; }

            public GeoValue(double longitude, double latitude, double maxDistanceMiles)
            {
                Coordinates = new double[] { longitude, latitude };
                MaxDistanceMiles = maxDistanceMiles;
            }
        }

        /// <summary>
        /// The name of the property on which to filter
        /// </summary>
        [JsonProperty(PropertyName = "property_name")]
        public string PropertyName { get; private set; }

        /// <summary>
        /// The filter operator to use
        /// </summary>
        [JsonProperty( PropertyName = "operator")]
        public FilterOperator Operator { get; private set; }

        /// <summary>
        /// The value to compare to the property specified in PropertyName
        /// </summary>
        [JsonProperty(PropertyName = "property_value")]
        public object Value { get; private set; }

        public QueryFilter(string property, FilterOperator op, object value)
        {
            if (string.IsNullOrWhiteSpace(property))
                throw new ArgumentNullException("property");
            if (null == value)
                throw new ArgumentNullException("value");

            PropertyName = property;
            Operator = op;
            Value = value;
        }
    }
}

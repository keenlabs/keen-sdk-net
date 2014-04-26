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
        /// <summary>
        /// Operators that may be used with a filter.
        /// </summary>
        public enum FilterOperator
        {
            /// <summary>
            /// Equal to.
            /// <para>Use with string, number, boolean</para>
            /// </summary>
            eq,

            /// <summary>
            /// Not equal to.
            /// <para>Use with string, number</para>
            /// </summary>
            ne,

            /// <summary>
            /// Less than.
            /// <para>Use with string, number</para>
            /// </summary>
            lt,

            /// <summary>
            /// Less than or equal to.
            /// <para>Use with number</para>
            /// </summary>
            lte,

            /// <summary>
            /// Greater than.
            /// <para>Use with string, number</para>
            /// </summary>
            gt,

            /// <summary>
            /// Greater than or equal to.
            /// <para>Use with number</para>
            /// </summary>
            gte,

            /// <summary>
            /// Whether a specific property exists on an event record.
            /// The Value property must be set to "true" or "false"
            /// <para>Use with string, number, boolean</para>
            /// </summary>
            exists,

            /// <summary>
            /// Whether the property value is in a give set of values.
            /// The Value property must be a JSON array of values, e.g.: "[1,2,4,5]"
            /// <para>Use with string, number, boolean</para>
            /// </summary>
            @in,

            /// <summary>
            /// Whether the property value contains the give set of characters.
            /// <para>Use with strings</para>
            /// </summary>
            contains,

            /// <summary>
            /// Used to select events within a certain radius of the provided geo coordinate.
            /// <para>Use with geo analysis</para>
            /// </summary>
            within,
        }

        /// <summary>
        /// Represents a value for a geo filter. Event coordinates must be recorded in the
        /// event property "keen.location".
        /// <para>Use with property "keen.location.coordinates" and operator "within"</para>
        /// </summary>
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
        [JsonProperty(PropertyName = "operator")]
        [JsonConverter(typeof(StringEnumConverter))]
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

using System.Collections.Generic;

namespace Keen.Core.Query
{
    using System;
    using System.Linq;
    using Dataset;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Holds information describing the query that is cached within a cached dataset.
    /// </summary>
    public class QueryDefinition
    {
        /// <summary>
        /// Unique id of the project to analyze.
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// The type of analysis for this query (e.g. count, count_unique, sum etc.)
        /// </summary>
        public string AnalysisType { get; set; }
        /// <summary>
        /// Specifies the name of the event collection to analyze.
        /// </summary>
        public string EventCollection { get; set; }
        /// <summary>
        /// Refines the scope of events to be included in the analysis based on event property values.
        /// </summary>
        public IEnumerable<QueryFilter> Filters { get; set; }
        /// <summary>
        /// Limits analysis to a specific period of time when the events occurred.
        /// </summary>
        public string Timeframe { get; set; }
        /// <summary>
        /// Assigns a timezone offset to relative timeframes.
        /// </summary>
        public string Timezone { get; set; }
        /// <summary>
        /// Specifies the size of time interval by which to group results. Using this parameter changes the response format.
        /// </summary>
        public string Interval { get; set; }
        /// <summary>
        /// Specifies the name of a property by which to group results. Using this parameter changes the response format.
        /// </summary>
        public IEnumerable<string> GroupBy { get; set; }
    }

    internal static class QueryDefinitionExtensions
    {
        public static void ValidateForCachedDataset(this QueryDefinition query)
        {
            if (string.IsNullOrWhiteSpace(query.AnalysisType))
            {
                throw new KeenException("QueryDefinition must have an analysis type");
            }

            if (string.IsNullOrWhiteSpace(query.EventCollection))
            {
                throw new KeenException("QueryDefinition must specify an event collection");
            }

            if (string.IsNullOrWhiteSpace(query.Timeframe))
            {
                throw new KeenException("QueryDefinition must specify a timeframe");
            }

            if (string.IsNullOrWhiteSpace(query.Interval))
            {
                throw new KeenException("QueryDefinition must specify an interval");
            }
        }
    }

    /// <summary>
    /// This is used because the PUT endpoint for a dataset take a string for group_by, but return an array of strings.
    /// </summary>
    internal class QueryDefinitionConverter : JsonConverter
    {
        /* This prevents JToken.ToObject form recursively calling ReadJson until the stack runs out */
        private bool _isNested;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _isNested = true;
            var query = JToken.FromObject(value, serializer);
            var groupByList = query["group_by"]?.ToArray();

            if (groupByList != null)
            {
                query["group_by"] = groupByList.FirstOrDefault()?.ToString();
            }
            
            serializer.Serialize(writer, query);
            _isNested = false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type != JTokenType.Object)
                return null;

            _isNested = true;
            var query = token.ToObject<QueryDefinition>(serializer);
            _isNested = false;

            return query;
        }

        public override bool CanConvert(Type objectType)
        {
            if (_isNested)
                return false;

            return objectType == typeof(QueryDefinition);
        }
    }
}

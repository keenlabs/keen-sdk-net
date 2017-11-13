using System;
using System.Collections.Generic;
using System.Linq;
using Keen.Core;
using Keen.Query;


namespace Keen.Dataset
{
    public class DatasetDefinition
    {
        /// <summary>
        /// Name of the dataset, which is used as an identifier. Must be unique per project.
        /// </summary>
        public string DatasetName { get; set; }

        /// <summary>
        /// The human-readable string name for your Cached Dataset.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Holds information describing the query which is cached by this Cached Dataset.
        /// </summary>
        public QueryDefinition Query { get; set; }

        /// <summary>
        /// When the most recent computation was queued.
        /// </summary>
        public DateTime? LastScheduledDate { get; set; }

        /// <summary>
        /// The most recent interval that has been computed for the Cached Dataset.
        /// </summary>
        public DateTime? LatestSubtimeframeAvailable { get; set; }

        /// <summary>
        /// The difference between now and the most recent datapoint computed.
        /// </summary>
        public long MillisecondsBehind { get; set; }

        /// <summary>
        /// The event property name of string values results are retrieved by.
        /// </summary>
        public IEnumerable<string> IndexBy { get; set; }
    }

    internal static class DatasetDefinitionExtensions
    {
        public static void Validate(this DatasetDefinition dataset)
        {
            if (string.IsNullOrWhiteSpace(dataset.DatasetName))
            {
                throw new KeenException("DatasetDefinition must have a name.");
            }

            if (string.IsNullOrWhiteSpace(dataset.DisplayName))
            {
                throw new KeenException("DatasetDefinition must have a display name.");
            }

            if (null == dataset.IndexBy ||
                string.IsNullOrWhiteSpace(dataset.IndexBy.FirstOrDefault()))
            {
                throw new KeenException("DatasetDefinition must specify a property by which to " +
                                        "index.");
            }

            if (null == dataset.Query)
            {
                throw new KeenException("DatasetDefinition must contain a query to be cached.");
            }

            dataset.Query.ValidateForCachedDataset();
        }
    }
}

using System;
using System.Collections.Generic;
using Keen.Query;


namespace Keen.AccessKey
{
    // TODO : We should provide some helpers/constants/factories/builders/validation to
    // make it a littler easier to put together this model structure. For example, we could
    // easily provide an enum for 'Permitted' so that it's easy to use and self-documenting.
    // Actually, when serializing this SDK could populate 'Permitted' automatically based on
    // the 'Options' object if it weren't for the 'schema' permission, which would still be
    // easy to work around.

    /// <summary>
    /// Model for AccessKey object
    /// </summary>
    public class AccessKeyDefinition
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public ISet<string> Permitted { get; set; }
        public string Key { get; set; }
        public Options Options { get; set; }
    }

    /// <summary>
    /// When SavedQueries are permitted, the Access Key will have access to run saved queries.
    /// </summary>
    public class SavedQueries
    {
        public ISet<string> Blocked { get; set; }
        public IEnumerable<QueryFilter> Filters { get; set; }
        public ISet<string> Allowed { get; set; }
    }

    /// <summary>
    /// When Queries are permitted, the Access Key will have the ability to do ad-hoc queries.
    /// </summary>
    public class Queries
    {
        public IEnumerable<QueryFilter> Filters { get; set; }
    }

    /// <summary>
    /// When Writes are permitted, the Access Key will have the ability to stream data to Keen.
    /// </summary>
    public class Writes
    {
        public dynamic Autofill { get; set; } // "customer": { "id": "93iskds39kd93id", "name": "Ada Corp." }
    }

    /// <summary>
    /// When Datasets are permitted, the Access Key will have access to getting a dataset definition, retrieving cached dataset results, and listing cached datasets definitions for a project.
    /// </summary>
    public class Datasets
    {
        public IEnumerable<string> Operations { get; set; }
        public IDictionary<string, AllowedDatasetIndexes> Allowed { get; set; }
        public ISet<string> Blocked { get; set; }
    }

    /// <summary>
    /// Optionals limiting of allowed datasets in the access key by index
    /// </summary>
    public class AllowedDatasetIndexes
    {
        // TODO: It seems like this should probably be a
        // Tuple<string, IEnumerable<string>> as per examples:
        //
        // "allowed": {
        //   "my_single_index_dataset": {
        //     "index_by": {
        //       "customer.id": ["93iskds39kd93id"]
        //     }
        //   },
        //   "my_other_dataset_unlimited_access": {}
        // }
        //
        // If at some point we can specify >1 index_by property name, each with various values,
        // this will need to be an IDictionary<string, IEnumerable<string>> instead.
        public Tuple<string, string> IndexBy { get; set; }
    }

    /// <summary>
    /// When CachedQueries are permitted, the Access Key will have access to retrieve results from cached queries.
    /// </summary>
    public class CachedQueries
    {
        public ISet<string> Blocked { get; set; }
        public ISet<string> Allowed { get; set; }
    }

    /// <summary>
    /// An object containing more details about the key’s permitted and restricted functionality.
    /// </summary>
    public class Options
    {
        public SavedQueries SavedQueries { get; set; }
        public Writes Writes { get; set; }
        public Datasets Datasets { get; set; }
        public CachedQueries CachedQueries { get; set; }
        public Queries Queries { get; set; }
    }
}

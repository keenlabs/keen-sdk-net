using Keen.Core.Query;
using System;
using System.Collections.Generic;

namespace Keen.Core.AccessKey
{
    /// <summary>
    /// Model for AccessKey object
    /// </summary>
    public class AccessKey
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
        public Tuple<string, string> IndexBy { get;  set;}
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

using Keen.Core.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core.AccessKey
{
    public class AccessKey
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public List<string> Permitted { get; set; }
        public string Key { get; set; }
        public string ProjectId { get; set; }
        public Options Options { get; set; }
    }

    public class SavedQueries
    {
        public string[] Blocked { get; set; }
        public IEnumerable<QueryFilter> Filters { get; set; }
        public string[] Allowed { get; set; }
    }

    public class Autofill
    {
        public IEnumerable<IDictionary<string, string>> Filters { get; set; }
    }

    public class Writes
    {
        public dynamic autofill { get; set; } // "customer": { "id": "93iskds39kd93id", "name": "Ada Corp." }
    }

    public class Datasets
    {
        public string[] Operations { get; set; }
        public IEnumerable<IDictionary<string, string>> Allowed { get; set; } //may be this also dynamic?
        public string[] Blocked { get; set; }
    }

    public class CachedQueries
    {
        public string[] Blocked { get; set; }
        public string[] Allowed { get; set; }
    }

    public class Options
    {
        public SavedQueries SavedQueries { get; set; }
        public Writes Writes { get; set; }
        public Datasets Datasets { get; set; }
        public CachedQueries CachedQueries { get; set; }
        public IQueries Queries { get; set; }
    }
}

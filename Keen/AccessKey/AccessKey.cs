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
        public IEnumerable<string> Permitted { get; set; }
        public string Key { get; set; }
        public Options Options { get; set; }
    }

    public class SavedQueries
    {
        public IEnumerable<string> Blocked { get; set; }
        public IEnumerable<QueryFilter> Filters { get; set; }
        public IEnumerable<string> Allowed { get; set; }
    }

    public class Quaries
    {
        public IEnumerable<QueryFilter> Filters { get; set; }
    }

    public class Writes
    {
        public dynamic Autofill { get; set; } // "customer": { "id": "93iskds39kd93id", "name": "Ada Corp." }
    }

    public class Datasets
    {
        public IEnumerable<string> Operations { get; set; }
        public IDictionary<string, IDictionary<string, IDictionary<string, string>>> Allowed { get; set; }
        public IEnumerable<string> Blocked { get; set; }
    }

    public class CachedQueries
    {
        public IEnumerable<string> Blocked { get; set; }
        public IEnumerable<string> Allowed { get; set; }
    }

    public class Options
    {
        public SavedQueries SavedQueries { get; set; }
        public Writes Writes { get; set; }
        public Datasets Datasets { get; set; }
        public CachedQueries CachedQueries { get; set; }
        public Quaries Queries { get; set; }
    }
}

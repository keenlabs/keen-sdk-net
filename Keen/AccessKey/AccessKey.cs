using Keen.Core.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keen.Core.AccessKey
{
    public class AccessKey
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public ISet<string> Permitted { get; set; }
        public string Key { get; set; }
        public Options Options { get; set; }

        internal AccessKey(string name, bool isActive, ISet<string> permitted, Options options)
        {
            this.Key = null; //Not needed for the creation of key
            this.Name = name;
            this.IsActive = isActive;
            this.Permitted = permitted;
            this.Options = options;
        }

        
    }

    public class SavedQueries
    {
        public ISet<string> Blocked { get; set; }
        public IEnumerable<QueryFilter> Filters { get; set; }
        public ISet<string> Allowed { get; set; }
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
        public IDictionary<string, AllowedDatasetIndexes> Allowed { get; set; }
        public ISet<string> Blocked { get; set; }
    }

    public class AllowedDatasetIndexes
    {
        public Tuple<string, string> IndexBy { get;  set;}
    }

    public class CachedQueries
    {
        public ISet<string> Blocked { get; set; }
        public ISet<string> Allowed { get; set; }
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

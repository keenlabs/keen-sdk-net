using System.Collections.Generic;

namespace Keen.Core.Dataset
{
    using Newtonsoft.Json;

    public class DatasetRequest
    {
        public IEnumerable<string> IndexBy { get; set; }
        public string Timeframe { get; set; }
    }
}

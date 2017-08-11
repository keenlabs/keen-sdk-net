using System.Collections.Generic;

namespace Keen.Core.Dataset
{
    public class DatasetRequest
    {
        public IEnumerable<string> IndexBy { get; set; }
        public string Timeframe { get; set; }
    }
}

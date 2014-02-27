using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.NET
{
    /// <summary>
    /// Values required to access a Keen project
    /// </summary>
    public class ProjectSettings
    {
        /// <summary>
        /// The Project ID, identifying the data silo to be accessed.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// The Master API key, required for getting a collection schema
        /// or deleting the entire event collection.
        /// </summary>
        public string MasterKey { get; set; }

        /// <summary>
        /// The Write API key, required for inserting events.
        /// </summary>
        public string WriteKey { get; set; }

        /// <summary>
        /// The Read API key, used with query requests.
        /// </summary>
        public string ReadKey { get; set; }
    }
}

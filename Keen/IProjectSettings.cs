using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keen.Core
{
    /// <summary>
    /// Values required to access a Keen project
    /// </summary>
    public interface IProjectSettings
    {
        /// <summary>
        /// The Project ID, identifying the data silo to be accessed.
        /// </summary>
        string ProjectId { get; }

        /// <summary>
        /// The Master API key, required for getting a collection schema
        /// or deleting the entire event collection.
        /// </summary>
        string MasterKey { get; }

        /// <summary>
        /// The Write API key, required for inserting events.
        /// </summary>
        string WriteKey { get; }

        /// <summary>
        /// The Read API key, used with query requests.
        /// </summary>
        string ReadKey { get; }
    }
}

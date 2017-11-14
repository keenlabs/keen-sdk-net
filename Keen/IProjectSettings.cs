
namespace Keen.Core
{
    /// <summary>
    /// Values required to access a Keen project
    /// </summary>
    public interface IProjectSettings
    {
        /// <summary>
        /// The Keen.IO URL for this project. Usually this will be the
        /// server address and API version. This should end with a '/'.
        /// 
        /// - e.g. https://api.keen.io/3.0/
        /// 
        /// </summary>
        string KeenUrl { get; }

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

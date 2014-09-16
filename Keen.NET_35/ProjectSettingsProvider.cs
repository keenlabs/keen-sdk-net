using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Keen.NET_35
{
    public class ProjectSettingsProvider : IProjectSettings
    {
        /// <summary>
        /// The Keen.IO URL for this project. Usually this will be the
        /// server address and API version.
        /// </summary>
        public string KeenUrl { get; protected set; }
        
        /// <summary>
        /// The Project ID, identifying the data silo to be accessed.
        /// </summary>
        public string ProjectId { get; protected set; }

        /// <summary>
        /// The Master API key, required for getting a collection schema
        /// or deleting the entire event collection.
        /// </summary>
        public string MasterKey { get; protected set; }

        /// <summary>
        /// The Write API key, required for inserting events.
        /// </summary>
        public string WriteKey { get; protected set; }

        /// <summary>
        /// The Read API key, used with query requests.
        /// </summary>
        public string ReadKey { get; protected set; }

        /// <summary>
        /// Obtains project setting values as constructor parameters
        /// </summary>
        /// <param name="keenUrl">Base Keen.IO service URL, required</param>
        /// <param name="projectId">Keen project id, required</param>
        /// <param name="masterKey">Master API key, required for getting schema or deleting collections</param>
        /// <param name="writeKey">Write API key, required for inserting events</param>
        /// <param name="readKey">Read API key</param>
        public ProjectSettingsProvider(string projectId, string masterKey = "", string writeKey = "", string readKey = "", string keenUrl = null)
        {
            KeenUrl = keenUrl ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/";
            ProjectId = projectId;
            MasterKey = masterKey;
            WriteKey = writeKey;
            ReadKey = readKey;
        }

        protected ProjectSettingsProvider()
        {
        }

        public override string ToString()
        {
            return string.Format("ProjectSettingsProviderEnv:{{\nKeenUrl:{0}; \nProjectId:{1}; \nMasterKey:{2}; \nWriteKey:{3}; \nReadKey:{4};\n}}",
                KeenUrl, ProjectId, MasterKey, WriteKey, ReadKey);
        }
    }
}

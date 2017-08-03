using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

namespace Keen.NetStandard
{
    /// <summary>
    /// Project settings provider which reads project settings from a text file.
    /// </summary>
    public class ProjectSettingsProviderFile : ProjectSettingsProvider
    {
        /// <summary>
        /// <para>Reads project settings from a json formatted file with a root object containing the below keys.</para>
        /// <para>KEEN_PROJECT_ID is required, along with at least one access key.</para>
        /// <para>KEEN_PROJECT_ID key should contain the Project Id. Required.</para>
        /// <para>KEEN_MASTER_KEY key should contain the Master Key. Optional, and highly discouraged unless using APIs that require a master key.</para>
        /// <para>KEEN_WRITE_KEY key should contain the Write Key. Optional, though at least one access key is required.</para>
        /// <para>KEEN_READ_KEY key should contain the ReadKey. Optional, though at least one access key is required.</para>
        /// <para>KEEN_SERVER_URL key should contain the Keen.IO API url. Optional.</para>
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        public ProjectSettingsProviderFile(string filePath)
        {
            try
            {
                // http://www.newtonsoft.com/json/help/html/ReadJson.htm
                JObject jsonProjectSettings = JObject.Parse(File.ReadAllText(filePath));
                
                Initialize(
	                (string)jsonProjectSettings[KeenConstants.KeenProjectId],
	                (string)jsonProjectSettings[KeenConstants.KeenMasterKey],
	                (string)jsonProjectSettings[KeenConstants.KeenWriteKey],
	                (string)jsonProjectSettings[KeenConstants.KeenReadKey],
	                (string)jsonProjectSettings[KeenConstants.KeenServerUrl]);
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                throw new KeenException("Failed to read configuration file.",
                                        ex);
            }
        }
    }
}

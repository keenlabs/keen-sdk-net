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
        private static string _fileKeenUrl { get; set; }
        private static string _fileProjectId { get; set; }
        private static string _fileMasterKey { get; set; }
        private static string _fileWriteKey { get; set; }
        private static string _fileReadKey { get; set; }

        /// <summary>
        /// <para>Reads the project settings from a text file.</para>
        /// <para>Each setting takes one line, in the order: Project ID, Master Key, Write Key,
        /// Read Key, Keen.IO API url. Unused values should be represented
        /// with a blank line.</para>
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        public ProjectSettingsProviderFile(string filePath)
            : base(SetSettingsFromFile(filePath, false),
                    masterKey: _fileMasterKey,
                    writeKey: _fileWriteKey,
                    readKey: _fileReadKey,
                    keenUrl: _fileKeenUrl)
        {

        }

        /// <summary>
        /// <para>If the isJsonFile parameter is set to true, then the key value pairs should be in
        /// a JSON object.</para>
        /// <para>KEEN_PROJECT_ID key should contain the Project Id</para>
        /// <para>KEEN_MASTER_KEY key should contain the Master Key</para>
        /// <para>KEEN_WRITE_KEY key should contain the Write Key</para>
        /// <para>KEEN_READ_KEY key should contain the ReadKey</para>
        /// <para>KEEN_SERVER_URL key should contain the Keen.IO API url</para>
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="isJsonFile">Indicates whether the file should be parsed as JSON</param>
        public ProjectSettingsProviderFile(string filePath, bool isJsonFile = false)
            : base(SetSettingsFromFile(filePath, isJsonFile),
            masterKey: _fileMasterKey,
            writeKey: _fileWriteKey,
            readKey: _fileReadKey,
            keenUrl: _fileKeenUrl)
        {

        }

        /// <summary>
        /// Maps the file contents onto the private variables to then be used by the constructor
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="isJsonFile">Indicates whether the file should be parsed as JSON</param>
        /// <returns>The ProjectId</returns>
        private static string SetSettingsFromFile(string filePath, bool isJsonFile = false)
        {
            if (isJsonFile)
            {
                // http://www.newtonsoft.com/json/help/html/ReadJson.htm
                JObject jsonProjectSettings = JObject.Parse(File.ReadAllText(filePath));

                _fileKeenUrl = (string)jsonProjectSettings[KeenConstants.KeenServerUrl];
                _fileProjectId = (string)jsonProjectSettings[KeenConstants.KeenProjectId];
                _fileMasterKey = (string)jsonProjectSettings[KeenConstants.KeenMasterKey];
                _fileWriteKey = (string)jsonProjectSettings[KeenConstants.KeenWriteKey];
                _fileReadKey = (string)jsonProjectSettings[KeenConstants.KeenReadKey];
            }
            else
            {
                // TODO : Master key maybe should be de-emphasized and not be first.
                var values = File.ReadLines(filePath).ToList();                

                _fileProjectId = values.ElementAtOrDefault(0);
                _fileMasterKey = values.ElementAtOrDefault(1);
                _fileWriteKey = values.ElementAtOrDefault(2);
                _fileReadKey = values.ElementAtOrDefault(3);
                _fileKeenUrl = values.ElementAtOrDefault(4);
            }

            return _fileProjectId;
        }
    }
}

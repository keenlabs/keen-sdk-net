using Keen.Core;
using System.IO;


namespace Keen.Net
{
    /// <summary>
    /// Project settings provider which reads project settings from a text file.
    /// </summary>
    public class ProjectSettingsProviderFile : ProjectSettingsProvider
    {
        /// <summary>
        /// <para>Reads the project settings from a text file.</para>
        /// <para>Each setting takes one line, in the order Project ID, 
        /// Master Key, Write Key, Read Key. Unused values should be represented
        /// with a blank line.</para>
        /// </summary>
        public ProjectSettingsProviderFile(string filePath)
        {
            // TODO : Add Keen Server URL as one of the lines, optionally.
            // TODO : Master key maybe should be de-emphasized and not be first.
            // TODO : Share init of properties with base class implementation.
            var values = File.ReadAllLines(filePath);
            if (values.Length != 4)
                throw new KeenException("Invalid project settings file, file must contain exactly 4 lines: " + filePath);

            ProjectId = values[0];
            MasterKey = values[1];
            WriteKey = values[2];
            ReadKey = values[3];
        }
    }
}

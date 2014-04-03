using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Master Key, Write Key ReadKey. Unused values should be represented
        /// with a blank line.</para>
        /// </summary>
        public ProjectSettingsProviderFile(string filePath)
        {
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

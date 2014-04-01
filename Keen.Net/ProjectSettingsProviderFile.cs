using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Keen.Core;
using System.IO;

namespace Keen.net
{
    /// <summary>
    /// Project settings provider which reads project settings from a text file.
    /// </summary>
    public class ProjectSettingsProviderFile : ProjectSettingsProvider
    {
        /// <summary>
        /// <para>Reads the project settings from a text file.</para>
        /// <para>Each setting takes one line, Project ID should be in variable KEEN_PROJECT_ID</para>
        /// <para>Master Key should be in variable KEEN_MASTER_ID</para>
        /// <para>Write Key should be in variable KEEN_WRITE_ID</para>
        /// <para>ReadKey should be in variable KEEN_READ_ID</para>
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

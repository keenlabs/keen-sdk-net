using Keen.Core;
using System;


namespace Keen.Net
{
    /// <summary>
    /// Project settings provider which reads project settings from environment variables.
    /// </summary>
    public class ProjectSettingsProviderEnv : ProjectSettingsProvider
    {
        /// <summary>
        /// <para>Reads the project settings from environment variables</para>
        /// <para>Project ID should be in variable KEEN_PROJECT_ID</para>
        /// <para>Master Key should be in variable KEEN_MASTER_KEY</para>
        /// <para>Write Key should be in variable KEEN_WRITE_KEY</para>
        /// <para>ReadKey should be in variable KEEN_READ_KEY</para>
        /// <para>Keen.IO API url should be in variable KEEN_SERVER_URL</para>
        /// </summary>
        public ProjectSettingsProviderEnv()
        {
            KeenUrl = Environment.GetEnvironmentVariable("KEEN_SERVER_URL") ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/";
            ProjectId = Environment.GetEnvironmentVariable("KEEN_PROJECT_ID") ?? "";
            MasterKey = Environment.GetEnvironmentVariable("KEEN_MASTER_KEY") ?? "";
            WriteKey = Environment.GetEnvironmentVariable("KEEN_WRITE_KEY") ?? "";
            ReadKey = Environment.GetEnvironmentVariable("KEEN_READ_KEY") ?? "";
        }
    }
}

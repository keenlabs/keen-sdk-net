using System;

namespace Keen.NET_35
{
    /// <summary>
    /// Project settings provider which reads project settings from environment variables.
    /// </summary>
    public class ProjectSettingsProviderEnv : ProjectSettingsProvider
    {
        /// <summary>
        /// <para>Reads the project settings from environment variables</para>
        /// <para>Project ID should be in variable KEEN_PROJECT_ID</para>
        /// <para>Master Key should be in variable KEEN_MASTER_ID</para>
        /// <para>Write Key should be in variable KEEN_WRITE_ID</para>
        /// <para>ReadKey should be in variable KEEN_READ_ID</para>
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

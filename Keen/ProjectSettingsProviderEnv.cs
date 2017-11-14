using System;


namespace Keen.Core
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
            : base(Environment.GetEnvironmentVariable(KeenConstants.KeenProjectId) ?? null,
                   masterKey: Environment.GetEnvironmentVariable(KeenConstants.KeenMasterKey) ?? null,
                   writeKey: Environment.GetEnvironmentVariable(KeenConstants.KeenWriteKey) ?? null,
                   readKey: Environment.GetEnvironmentVariable(KeenConstants.KeenReadKey) ?? null,
                   keenUrl: Environment.GetEnvironmentVariable(KeenConstants.KeenServerUrl) ?? null)
        {

        }
    }
}

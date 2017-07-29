using System;
using System.Collections.Generic;
using System.Text;

namespace Keen.NetStandard
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
            : base(Environment.GetEnvironmentVariable(KeenConstants.KeenProjectId) ?? "",
                    masterKey: Environment.GetEnvironmentVariable(KeenConstants.KeenMasterKey) ?? "",
                    writeKey: Environment.GetEnvironmentVariable(KeenConstants.KeenWriteKey) ?? "",
                    readKey: Environment.GetEnvironmentVariable(KeenConstants.KeenReadKey) ?? "",
                    keenUrl: Environment.GetEnvironmentVariable(KeenConstants.KeenServerUrl) ?? KeenConstants.ServerAddress + "/" + KeenConstants.ApiVersion + "/")
        {

        }
    }
}

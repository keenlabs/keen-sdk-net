using System;
using NUnit.Framework;


namespace Keen.Core.Test
{
    public class TestBase
    {
        public static bool UseMocks = true;
        public IProjectSettings SettingsEnv;
        private static string[] s_environmentKeys = new[]
        {
            KeenConstants.KeenProjectId,
            KeenConstants.KeenMasterKey,
            KeenConstants.KeenWriteKey,
            KeenConstants.KeenReadKey
        };

        [OneTimeSetUp]
        public virtual void Setup()
        {
            if (UseMocks)
                SetupEnv();
            SettingsEnv = new ProjectSettingsProviderEnv();
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
            if (UseMocks)
                ResetEnv();
        }

        public static void SetupEnv()
        {
            foreach (var s in s_environmentKeys)
                Environment.SetEnvironmentVariable(s, "0123456789ABCDEF0123456789ABCDEF");
        }

        public static void ResetEnv()
        {
            foreach (var s in s_environmentKeys)
                Environment.SetEnvironmentVariable(s, null);
        }
    }
}

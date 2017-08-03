using System;
using NUnit.Framework;


namespace Keen.NetStandard.Test
{
    public class TestBase
    {
        public static bool UseMocks = true;
        public IProjectSettings SettingsEnv;

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
            foreach (var s in new[] { "KEEN_PROJECT_ID", "KEEN_MASTER_KEY", "KEEN_WRITE_KEY", "KEEN_READ_KEY" })
                Environment.SetEnvironmentVariable(s, "0123456789ABCDEF0123456789ABCDEF");
        }

        public static void ResetEnv()
        {
            foreach (var s in new[] { "KEEN_PROJECT_ID", "KEEN_MASTER_KEY", "KEEN_WRITE_KEY", "KEEN_READ_KEY" })
                Environment.SetEnvironmentVariable(s, null);
        }
    }
}
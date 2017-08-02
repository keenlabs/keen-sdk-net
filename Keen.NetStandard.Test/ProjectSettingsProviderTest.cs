using System;
using NUnit.Framework;


namespace Keen.NetStandard.Tests
{
    [TestFixture]
    class ProjectSettingsProviderTest
    {
        [Test]
        public void Settings_DefaultInputs_Success()
        {
            Assert.DoesNotThrow(() => new ProjectSettingsProvider("X", null));
        }

        [Test]
        public void Settings_AllNull_Success()
        {
            Assert.DoesNotThrow(() => new ProjectSettingsProvider(null));
        }

        [Test]
        public void SettingsProviderEnv_VarsNotSet_Throws()
        {
            Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, null);
            Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, null);
            Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, null);
            Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, null);

            var settings = new ProjectSettingsProviderEnv();
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }

        [Test]
        public void SettingsProviderEnv_VarsSet_Success()
        {
            Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, "X");
            Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, "X");
            Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, "X");
            Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, "X");

            var settings = new ProjectSettingsProviderEnv();
            Assert.DoesNotThrow(() => new KeenClient(settings));
        }
    }
}

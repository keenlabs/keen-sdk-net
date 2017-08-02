using System;
using System.IO;
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

        [Test]
        public void SettingsProviderEnv_VarsSet_SettingsAreCorrect()
        {
            var projectId = "projectId";
            var masterKey = "masterKey";
            var writeKey = "writeKey";
            var readKey = "readKey";

            Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, projectId);
            Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, masterKey);
            Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, writeKey);
            Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, readKey);

            var settings = new ProjectSettingsProviderEnv();
            Assert.AreEqual(settings.ProjectId, projectId, "Project id wasn't properly set");
            Assert.AreEqual(settings.MasterKey, masterKey, "Master key wasn't properly set");
            Assert.AreEqual(settings.WriteKey, writeKey, "Write key wasn't properly set");
            Assert.AreEqual(settings.ReadKey, readKey, "Read key wasn't properly set");
        }

        [Test]
        public void SettingsProviderFile_InvalidFile_Throws()
        {
            var fp = Path.GetTempFileName();
            try 
            {
                File.WriteAllText(fp, "X");

                Assert.Throws<KeenException>(() => new ProjectSettingsProviderFile(fp));
            }
            finally
            {
                File.Delete(fp);
            }
        }

        [Test]
        public void SettingsProviderFile_ValidFile_Success()
        {
            var fp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(fp, "X\nX\nX\nX");

                Assert.DoesNotThrow(() => new ProjectSettingsProviderFile(fp));
            }
            finally
            {
                File.Delete(fp);
            }
        }
    }
}

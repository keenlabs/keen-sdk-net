using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;


namespace Keen.Core.Test
{
    [TestFixture]
    class ProjectSettingsProviderTest
    {
        [Test]
        public void Settings_DefaultInputs_Success()
        {
            Assert.DoesNotThrow(() => new ProjectSettingsProvider("X", "Y"));
        }

        [Test]
        public void Settings_AllNull_Throws()
        {
            Assert.Throws<KeenException>(() => new ProjectSettingsProvider(null));
        }

        [Test]
        public void SettingsProviderEnv_VarsNotSet_Throws()
        {
            Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, null);
            Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, null);
            Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, null);
            Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, null);

            Assert.Throws<KeenException>(() => new ProjectSettingsProviderEnv());
        }

        [Test]
        public void SettingsProviderEnv_VarsSet_Success()
        {
            Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, "X");
            Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, "X");
            Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, "X");
            Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, "X");

            Assert.DoesNotThrow(() => new ProjectSettingsProviderEnv());
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
        public void SettingsProviderFile_NoKey_Throws()
        {
            var fileName = Path.GetTempFileName();
            try
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(
                    new Dictionary<string, string>
                    {
                        [KeenConstants.KeenProjectId] = "projectId"
                    }
                ));

                Assert.Throws<KeenException>(() => new ProjectSettingsProviderFile(fileName));
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Test]
        public void SettingsProviderFile_ValidMinimalConfig_Success()
        {
            var fileName = Path.GetTempFileName();
            try
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(
                    new Dictionary<string, string>
                    {
                        [KeenConstants.KeenProjectId] = "projectId",
                        [KeenConstants.KeenReadKey] = "readKey"
                    }
                ));

                Assert.DoesNotThrow(() => new ProjectSettingsProviderFile(fileName));
            }
            finally
            {
                File.Delete(fileName);
            }
        }

        [Test]
        public void SettingsProviderFile_ConfigIsCorrect_Success()
        {
            var projectId = "projectId";
            var readKey = "readKey";
            var writeKey = "writeKey";
            var masterKey = "masterKey";
            var baseUrl = "baseUrl";

            var fileName = Path.GetTempFileName();
            try
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(
                    new Dictionary<string, string>
                    {
                        [KeenConstants.KeenProjectId] = projectId,
                        [KeenConstants.KeenReadKey] = readKey,
                        [KeenConstants.KeenWriteKey] = writeKey,
                        [KeenConstants.KeenMasterKey] = masterKey,
                        [KeenConstants.KeenServerUrl] = baseUrl
                    }
                ));

                var settings = new ProjectSettingsProviderFile(fileName);
                Assert.AreEqual(settings.ProjectId, projectId);
                Assert.AreEqual(settings.ReadKey, readKey);
                Assert.AreEqual(settings.WriteKey, writeKey);
                Assert.AreEqual(settings.MasterKey, masterKey);
                Assert.AreEqual(settings.KeenUrl, baseUrl);
            }
            finally
            {
                File.Delete(fileName);
            }
        }
    }
}

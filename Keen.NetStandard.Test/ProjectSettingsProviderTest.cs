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
            var ProjectId = Environment.GetEnvironmentVariable(KeenConstants.KeenProjectId);
			var MasterKey = Environment.GetEnvironmentVariable(KeenConstants.KeenMasterKey);
			var WriteKey = Environment.GetEnvironmentVariable(KeenConstants.KeenWriteKey);
			var ReadKey = Environment.GetEnvironmentVariable(KeenConstants.KeenReadKey);

			try
			{
				Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, null);
				Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, null);
				Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, null);
				Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, null);

				var settings = new ProjectSettingsProviderEnv();
				Assert.Throws<KeenException>(() => new KeenClient(settings));
			}
			finally
			{
				Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, ProjectId);
				Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, MasterKey);
				Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, WriteKey);
				Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, ReadKey);
			}
		}

		[Test]
		public void SettingsProviderEnv_VarsSet_Success()
		{
			var ProjectId = Environment.GetEnvironmentVariable(KeenConstants.KeenProjectId);
			var MasterKey = Environment.GetEnvironmentVariable(KeenConstants.KeenMasterKey);
			var WriteKey = Environment.GetEnvironmentVariable(KeenConstants.KeenWriteKey);
			var ReadKey = Environment.GetEnvironmentVariable(KeenConstants.KeenReadKey);

			try
			{
				Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, "X");
				Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, "X");
				Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, "X");
				Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, "X");

				var settings = new ProjectSettingsProviderEnv();
				Assert.DoesNotThrow(() => new KeenClient(settings));
			}
			finally
			{
				Environment.SetEnvironmentVariable(KeenConstants.KeenProjectId, ProjectId);
				Environment.SetEnvironmentVariable(KeenConstants.KeenMasterKey, MasterKey);
				Environment.SetEnvironmentVariable(KeenConstants.KeenWriteKey, WriteKey);
				Environment.SetEnvironmentVariable(KeenConstants.KeenReadKey, ReadKey);
			}
		}
    }
}

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
			var ProjectId = Environment.GetEnvironmentVariable("KEEN_PROJECT_ID");
			var MasterKey = Environment.GetEnvironmentVariable("KEEN_MASTER_KEY");
			var WriteKey = Environment.GetEnvironmentVariable("KEEN_WRITE_KEY");
			var ReadKey = Environment.GetEnvironmentVariable("KEEN_READ_KEY");

			try
			{
				Environment.SetEnvironmentVariable("KEEN_PROJECT_ID", null);
				Environment.SetEnvironmentVariable("KEEN_MASTER_KEY", null);
				Environment.SetEnvironmentVariable("KEEN_WRITE_KEY", null);
				Environment.SetEnvironmentVariable("KEEN_READ_KEY", null);

				var settings = new ProjectSettingsProviderEnv();
				Assert.Throws<KeenException>(() => new KeenClient(settings));
			}
			finally
			{
				Environment.SetEnvironmentVariable("KEEN_PROJECT_ID", ProjectId);
				Environment.SetEnvironmentVariable("KEEN_MASTER_KEY", MasterKey);
				Environment.SetEnvironmentVariable("KEEN_WRITE_KEY", WriteKey);
				Environment.SetEnvironmentVariable("KEEN_READ_KEY", ReadKey);
			}
		}

		[Test]
		public void SettingsProviderEnv_VarsSet_Success()
		{
			var ProjectId = Environment.GetEnvironmentVariable("KEEN_PROJECT_ID");
			var MasterKey = Environment.GetEnvironmentVariable("KEEN_MASTER_KEY");
			var WriteKey = Environment.GetEnvironmentVariable("KEEN_WRITE_KEY");
			var ReadKey = Environment.GetEnvironmentVariable("KEEN_READ_KEY");

			try
			{
				Environment.SetEnvironmentVariable("KEEN_PROJECT_ID", "X");
				Environment.SetEnvironmentVariable("KEEN_MASTER_KEY", "X");
				Environment.SetEnvironmentVariable("KEEN_WRITE_KEY", "X");
				Environment.SetEnvironmentVariable("KEEN_READ_KEY", "X");

				var settings = new ProjectSettingsProviderEnv();
				Assert.DoesNotThrow(() => new KeenClient(settings));
			}
			finally
			{
				Environment.SetEnvironmentVariable("KEEN_PROJECT_ID", ProjectId);
				Environment.SetEnvironmentVariable("KEEN_MASTER_KEY", MasterKey);
				Environment.SetEnvironmentVariable("KEEN_WRITE_KEY", WriteKey);
				Environment.SetEnvironmentVariable("KEEN_READ_KEY", ReadKey);
			}
		}
    }
}

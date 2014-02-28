using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using Keen.Core;
using System.IO;

namespace Keen.NET.Test
{
    [TestFixture]
    public class ProjectSettingsProviderTest
    {
        [Test]
        public void SettingsProviderEnv_VarsNotSet_Throws()
        {
            Environment.SetEnvironmentVariable("KEEN_PROJECT_ID", null);
            Environment.SetEnvironmentVariable("KEEN_MASTER_KEY", null);
            Environment.SetEnvironmentVariable("KEEN_WRITE_KEY", null);
            Environment.SetEnvironmentVariable("KEEN_READ_KEY", null);

            var settings = new ProjectSettingsProviderEnv();
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }

        [Test]
        public void SettingsProviderEnv_VarsSet_Success()
        {
            Environment.SetEnvironmentVariable("KEEN_PROJECT_ID", "X");
            Environment.SetEnvironmentVariable("KEEN_MASTER_KEY", "X");
            Environment.SetEnvironmentVariable("KEEN_WRITE_KEY", "X");
            Environment.SetEnvironmentVariable("KEEN_READ_KEY", "X");

            var settings = new ProjectSettingsProviderEnv();
            Assert.DoesNotThrow(() => new KeenClient(settings));
        }

        [Test]
        public void SettingsProviderFile_InvalidFile_Throws()
        {
            var fp = Path.GetTempFileName();
            try 
	        {
                File.WriteAllText(fp, "X\nX");

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

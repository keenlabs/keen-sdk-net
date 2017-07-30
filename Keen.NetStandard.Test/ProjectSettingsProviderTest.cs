using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Keen.NetStandard.Tests
{
    [TestFixture]
    class ProjectSettingsProviderTest
    {
        [Test]
        public void Settings_DefaultInputs_Success()
        {
            Assert.DoesNotThrow(() => new ProjectSettingsProvider("X",null));
        }
        [Test]
        public void Settings_AllNull_Success()
        {
            Assert.DoesNotThrow(() => new ProjectSettingsProvider(null));
        }



    }
}

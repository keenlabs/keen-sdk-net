using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using Keen.NET;

namespace Keen.NET.Test
{
    [TestFixture]
    public class KeenClientTest
    {
        [Test]
        public void Constructor_ProjectSettingsNull_Throws()
        {
            Assert.Throws<KeenException>(() => new KeenClient(null));
        }

        [Test]
        public void Constructor_ProjectSettingsEmpty_Throws()
        {
            var settings = new ProjectSettings();
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }

        [Test]
        public void Constructor_ProjectSettingsNoProjectID_Throws()
        {
            var settings = new ProjectSettings() { MasterKey = "X", WriteKey = "X" };
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }

        [Test]
        public void Constructor_ProjectSettingsNoReadWriteKeys_Throws()
        {
            var settings = new ProjectSettings() { ProjectId = "X" };
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }

        [Test]
        public void Constructor_settings_Throws()
        {
            var settings = new ProjectSettings() { ProjectId = "X" };
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }
    }
}

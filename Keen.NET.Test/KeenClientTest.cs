using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using Keen.NET;
using System.IO;

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
        public void Constructor_ProjectSettingsNoProjectID_Throws()
        {
            var settings = new ProjectSettingsProvider(projectId: "", masterKey: "X", writeKey: "X");
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }

        [Test]
        public void Constructor_ProjectSettingsNoMasterOrWriteKeys_Throws()
        {
            var settings = new ProjectSettingsProvider(projectId: "X");
            Assert.Throws<KeenException>(() => new KeenClient(settings));
        }
    }
}

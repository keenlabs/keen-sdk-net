using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using Keen.Core;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections;
using System.Dynamic;

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

        [Test]
        public void GetCollectionSchema_NullProjectId_Throws()
        {
            var settingsEnv = new ProjectSettingsProviderEnv();
            var settings = new ProjectSettingsProvider(projectId: "X", masterKey: settingsEnv.MasterKey);
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.GetSchema(null));
        }

        [Test]
        public void GetCollectionSchema_EmptyProjectId_Throws()
        {
            var settingsEnv = new ProjectSettingsProviderEnv();
            var settings = new ProjectSettingsProvider(projectId: "X", masterKey: settingsEnv.MasterKey);
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.GetSchema(""));
        }


        [Test]
        public void GetCollectionSchema_InvalidProjectId_Throws()
        {
            var settingsEnv = new ProjectSettingsProviderEnv();
            var settings = new ProjectSettingsProvider(projectId: "X", masterKey: settingsEnv.MasterKey);
            var client = new KeenClient(settings);
            Assert.Throws<KeenResourceNotFoundException>(() => client.GetSchema("X"));
        }

        [Test]
        public void GetCollectionSchema_ValidProjectIdInvalidSchema_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenResourceNotFoundException>(() => client.GetSchema("DoesntExist"));
        }

        [Test]
        public void GetCollectionSchema_ValidProject_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);

            // setup, ensure that collection AddEventTest exists.
            Assert.DoesNotThrow(() => client.AddEvent("AddEventTest", new { AProperty = "AValue" }));
            dynamic response;
            Assert.DoesNotThrow(() => {
                response = client.GetSchema("AddEventTest");
                Assert.NotNull(response["properties"]);
                Assert.NotNull(response["properties"]["AProperty"]);
                Assert.True((string)response["properties"]["AProperty"]=="string");
            });

        }

        [Test]
        public void AddEvent_InvalidProjectId_Throws()
        {
            var settingsEnv = new ProjectSettingsProviderEnv();
            var settings = new ProjectSettingsProvider(projectId: "X", masterKey: settingsEnv.MasterKey);
            var client = new KeenClient(settings);
            Assert.Throws<KeenResourceNotFoundException>(() => client.AddEvent("X", new { X = "X" }));
        }

        [Test]
        public void AddEvent_ValidProjectIdInvalidWriteKey_Throws()
        {
            var settingsEnv = new ProjectSettingsProviderEnv();
            var settings = new ProjectSettingsProvider(projectId: settingsEnv.ProjectId, writeKey: "X");
            var client = new KeenClient(settings);
            Assert.Throws<KeenInvalidApiKeyException>(() => client.AddEvent("X", new { X = "X" }));
        }

        [Test]
        public void AddEvent_InvalidCollectionNameBlank_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddEvent("", new { AProperty = "AValue" }));
        }

        [Test]
        public void AddEvent_InvalidCollectionNameNull_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddEvent(null, new { AProperty = "AValue" }));
        }

        [Test]
        public void AddEvent_InvalidCollectionNameDollarSign_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddEvent("$Invalid", new { AProperty = "AValue" }));
        }

        [Test]
        public void AddEvent_InvalidCollectionNameTooLong_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddEvent(new String('X', 257), new { AProperty = "AValue" }));
        }

        [Test]
        public void AddEvent_InvalidCollectionRootKeen_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Debug.WriteLine("event json:" + JsonConvert.SerializeObject(new { keen = "AValue" }));
            Assert.Throws<KeenNamespaceTypeException>(() => client.AddEvent("X", new { keen = "AValue" }));
        }

        [Test]
        public void AddEvent_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.DoesNotThrow(() => client.AddEvent("AddEventTest", new { AProperty = "AValue" }));
        }
    }
}

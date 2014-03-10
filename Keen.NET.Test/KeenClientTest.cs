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
using System.Threading;
using Newtonsoft.Json.Linq;

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
            var settings = new ProjectSettingsProvider(projectId: "X", writeKey: settingsEnv.WriteKey);
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

        [Test]
        public void AddEvent_ScopedKeyWrite_Success()
        {
            var settingsEnv = new ProjectSettingsProviderEnv();
            var scope = "{\"timestamp\": \"2014-02-25T22:09:27.320082\", \"allowed_operations\": [\"write\"]}";
            var scopedKey = ScopedKey.EncryptString(settingsEnv.MasterKey, scope);
            var settings = new ProjectSettingsProvider(projectId: settingsEnv.ProjectId, writeKey: scopedKey);            
            
            var client = new KeenClient(settings);
            Assert.DoesNotThrow(() => client.AddEvent("AddEventTest", new { AProperty = "CustomKey" }));
        }

        [Test]
        public void AddEvent_MultipleEventsInvalidCollection_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            var collection = new
            {
                AddEventTest = from i in Enumerable.Range(1, 10)
                               select new { AProperty = "AValue" + i },
                InvalidCollection = 2,
            };
            Assert.Throws<KeenInternalServerErrorException>(() => client.AddEvents(collection));
        }

        [Test]
        public void AddEvent_MultipleEventsAnonymous_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            var collection = new
            {
                AddEventTest = from i in Enumerable.Range(1, 10)
                               select new { AProperty = "AValue" + i }
            };
            Assert.DoesNotThrow(() => client.AddEvents(collection));
        }

        [Test]
        public void AddEvent_MultipleEventsExpando_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);

            dynamic collection = new ExpandoObject();
            collection.AddEventTest = new List<dynamic>();
            foreach( var i in Enumerable.Range(1,10))
            {
                dynamic anEvent = new ExpandoObject();
                anEvent.AProperty = "AValue" + i;
                collection.AddEventTest.Add(anEvent);
            }

            Assert.DoesNotThrow(() => client.AddEvents(collection));
        }

        private class TestCollection
        {
            public class TestEvent
            {
                public string AProperty { get; set; }
            }
            public List<TestEvent> AddEventTest { get; set; }
        }

        [Test]
        public void AddEvent_MultipleEvents_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);

            var collection = new TestCollection()
            {
                AddEventTest = (from i in Enumerable.Range(1, 10)
                               select new TestCollection.TestEvent() { AProperty = "AValue"+i}).ToList()
            };

            Assert.DoesNotThrow(() => client.AddEvents(collection));
        }

        [Test]
        public void DeleteCollection_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            // Idempotent, does not matter if collection does not exist.
            Assert.DoesNotThrow(() => client.DeleteCollection("DeleteColTest"));
        }

        [Test]
        public void AddGlobalProperty_SimpleValue_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.DoesNotThrow(() =>
                {
                    client.AddGlobalProperty("AGlobal", "AValue");
                    client.AddEvent("AddEventTest", new { AProperty = "AValue" });
                });

        }

        [Test]
        public void AddGlobalProperty_InvalidValueNameDollar_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddGlobalProperty("$AGlobal", "AValue"));
        }

        [Test]
        public void AddGlobalProperty_InvalidValueNamePeriod_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddGlobalProperty("A.Global", "AValue"));
        }

        [Test]
        public void AddGlobalProperty_InvalidValueNameLength_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddGlobalProperty(new String('A', 256), "AValue"));
        }

        [Test]
        public void AddGlobalProperty_InvalidValueNameNull_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddGlobalProperty(null, "AValue"));
        }


        [Test]
        public void AddGlobalProperty_InvalidValueNameBlank_Throws()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.Throws<KeenException>(() => client.AddGlobalProperty("", "AValue"));
        }

        [Test]
        public void AddGlobalProperty_ObjectValue_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.DoesNotThrow(() =>
            {
                client.AddGlobalProperty("AGlobal", new { AProperty = "AValue" });
                client.AddEvent("AddEventTest", new { AProperty = "AValue" });
            });

        }

        [Test]
        public void AddGlobalProperty_CollectionValue_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var client = new KeenClient(settings);
            Assert.DoesNotThrow(() =>
            {
                client.AddGlobalProperty("AGlobal", new []{ 1, 2, 3, });
                client.AddEvent("AddEventTest", new { AProperty = "AValue" });
            });

        }

    }
}

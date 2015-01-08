using System;
using System.Collections.Generic;
using Keen.Core;
using Keen.Core.DataEnrichment;
using Keen.Net.Test;
using NUnit.Framework;

namespace Keen.NET.Test
{
    [TestFixture]
    public class AddOnsTest : TestBase
    {
        [Test]
        public void IpToGeo_Send_Success()
        {
            var client = new KeenClient(SettingsEnv);
            if (UseMocks)
                client.EventCollection = new EventCollectionMock(SettingsEnv,
                    addEvent: (c, e, p) =>
                    {
                        if (!e["keen"].ToString().Contains("keen:ip_to_geo"))
                            throw new Exception("Unexpected values");
                    });

            var a = AddOn.IpToGeo("an_ip", "geocode");

            Assert.DoesNotThrow(() => client.AddEvent("AddOnTest", new {an_ip = "70.187.8.97"}, new List<AddOn> {a}));
        }

        [Test]
        public void IpToGeo_MissingInput_Throws()
        {
            var client = new KeenClient(SettingsEnv);
            if (UseMocks)
                client.EventCollection = new EventCollectionMock(SettingsEnv,
                    addEvent: (c, e, p) =>
                    {
                        if (!e["keen"].ToString().Contains("\"ip\": \"an_ip\""))
                            throw new KeenException("Unexpected values");
                    });

            var a = AddOn.IpToGeo("wrong_field", "geocode");

            Assert.Throws<KeenException>(() => client.AddEvent("AddOnTest", new { an_ip = "70.187.8.97" }, new List<AddOn> { a }));
        }


        [Test]
        public void UserAgentParser_Send_Success()
        {
            var client = new KeenClient(SettingsEnv);
            if (UseMocks)
                client.EventCollection = new EventCollectionMock(SettingsEnv,
                    addEvent: (c, e, p) =>
                    {
                        if (!e["keen"].ToString().Contains("keen:ua_parser"))
                            throw new Exception("Unexpected values");
                    });

            var a = AddOn.UserAgentParser("user_agent_string", "user_agent_parsed");

            Assert.DoesNotThrow(() => client.AddEvent("AddOnTest", new { user_agent_string = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36" }, new List<AddOn> { a }));
        }

        [Test]
        public void UrlParser_Send_Success()
        {
            var client = new KeenClient(SettingsEnv);
            if (!UseMocks)
                client.EventCollection = new EventCollectionMock(SettingsEnv,
                    addEvent: (c, e, p) =>
                    {
                        if (!e["keen"].ToString().Contains("keen:url_parser"))
                            throw new Exception("Unexpected values");
                    });

            var a = AddOn.UrlParser("url", "url_parsed");

            Assert.DoesNotThrow(() => client.AddEvent("AddOnTest", new { url = "https://keen.io/docs/data-collection/data-enrichment/#anchor" }, new List<AddOn> { a }));
        }

        [Test]
        public void ReferrerParser_Send_Success()
        {
            var client = new KeenClient(SettingsEnv);
            if (UseMocks)
                client.EventCollection = new EventCollectionMock(SettingsEnv,
                    addEvent: (c, e, p) =>
                    {
                        if (!e["keen"].ToString().Contains("keen:url_parser"))
                            throw new Exception("Unexpected values");
                    });

            var a = AddOn.ReferrerParser("referrer", "page", "referrer_parsed");

            Assert.DoesNotThrow(() => client.AddEvent("AddOnTest", new { page = "", referrer = "" }, new List<AddOn> { a }));
        }

    }
}

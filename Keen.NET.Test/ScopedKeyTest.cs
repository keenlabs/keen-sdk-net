using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Keen.Core;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Dynamic;
using System.Collections;

namespace Keen.Net.Test
{
    [TestFixture]
    public class ScopedKeyTest : TestBase
    {
        [Test]
        public void Encrypt_NullObject_Success()
        {
            Assert.DoesNotThrow(() => ScopedKey.Encrypt("abc", null));
        }

        [Test]
        public void Encrypt_NullKey_Success()
        {
            Assert.DoesNotThrow(() => ScopedKey.Encrypt(null, new { X = "X" }));
        }

        public void Encrypt_BlankKey_Success()
        {
            Assert.DoesNotThrow(() => ScopedKey.Encrypt("", new { X = "X" }));
        }

        [Test]
        public void Encrypt_PopulatedObject_Success()
        {
            Assert.DoesNotThrow(() =>
            {
                var settings = new ProjectSettingsProviderEnv();

                dynamic secOps = new ExpandoObject();

                IDictionary<string, object> filter = new ExpandoObject();
                filter.Add("property_name", "account_id" );
                filter.Add("operator", "eq" );
                filter.Add("property_value", 123 );
                secOps.filters = new List<object>(){ filter };
                secOps.allowed_operations = new List<string>(){ "read" };

                var scopedKey = ScopedKey.Encrypt(settings.MasterKey, (object)secOps);
            });
        }

        [Test]
        public void RoundTrip_PopulatedObject_Success()
        {
            var settings = new ProjectSettingsProviderEnv();

            IDictionary<string, object> filter = new ExpandoObject();
            filter.Add("property_name", "account_id");
            filter.Add("operator", "eq");
            filter.Add("property_value", 123);

            dynamic secOpsIn = new ExpandoObject();
            secOpsIn.filters = new List<object>() { filter };
            secOpsIn.allowed_operations = new List<string>() { "read" };
            Assert.DoesNotThrow(() =>
            {
                var scopedKey = ScopedKey.Encrypt(settings.MasterKey, (object)secOpsIn);
                var decrypted = ScopedKey.Decrypt(settings.MasterKey, scopedKey);
                var secOpsOut = JObject.Parse(decrypted); 
                Assert.True(secOpsIn.allowed_operations[0] == (string)(secOpsOut["allowed_operations"].First()));
            });
        }

        [Test]
        public void RoundTrip_PopulatedObject_WithIV_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            var IV = "C0FFEEC0FFEEC0FFEEC0FFEEC0FFEEC0";

            IDictionary<string, object> filter = new ExpandoObject();
            filter.Add("property_name", "account_id");
            filter.Add("operator", "eq");
            filter.Add("property_value", 123);

            dynamic secOpsIn = new ExpandoObject();
            secOpsIn.filters = new List<object>() { filter };
            secOpsIn.allowed_operations = new List<string>() { "read" };
            Assert.DoesNotThrow(() =>
            {
                var scopedKey = ScopedKey.Encrypt(settings.MasterKey, (object)secOpsIn, IV);
                var decrypted = ScopedKey.Decrypt(settings.MasterKey, scopedKey);
                var secOpsOut = JObject.Parse(decrypted);
                Assert.True(secOpsIn.allowed_operations[0] == (string)(secOpsOut["allowed_operations"].First()));
            });
        }

        [Test]
        public void Decrypt_WriteKey_Success() 
        {
            // if mocking is turned on, the write key will be fake and not decryptable, so skip the test
            if (UseMocks)
                return;

            var settings = new ProjectSettingsProviderEnv();
            Assert.DoesNotThrow(() => ScopedKey.Decrypt(settings.MasterKey, settings.WriteKey));
        }

        [Test]
        public void Decrypt_WriteKey()
        {
            var decrypted = ScopedKey.Decrypt(SettingsEnv.MasterKey, SettingsEnv.ReadKey);
            Trace.WriteLine(decrypted);
        }

        [Test]
        public void Roundtrip_RndIV_Success()
        {
            const string vendorGuid = "abc";
            const bool isRead = false;

            var str = "{\"filters\": [{\"property_name\": \"vendor_id\",\"operator\": \"eq\",\"property_value\": \"VENDOR_GUID\"}],\"allowed_operations\": [ \"READ_OR_WRITE\" ]}";

            str = str.Replace("VENDOR_GUID", vendorGuid);

            str = str.Replace("READ_OR_WRITE", isRead ? "read" : "write");

            var rnd = new System.Security.Cryptography.RNGCryptoServiceProvider();
            var bytes = new byte[16];
            rnd.GetBytes(bytes);
            
            var iv = String.Concat(bytes.Select(b => b.ToString("X2"))); Trace.WriteLine("IV: " + iv);

            var scopedKey = ScopedKey.EncryptString(SettingsEnv.MasterKey, str, iv );
            var decrypted = ScopedKey.Decrypt(SettingsEnv.MasterKey, scopedKey);
            Trace.WriteLine("decrypted: " + decrypted);

            var settings = new ProjectSettingsProvider(projectId: SettingsEnv.ProjectId, writeKey: scopedKey);
            var client = new KeenClient(settings);
            client.AddEvent("X", new { vendor_id = "abc", X = "123" });
        }
    }
}

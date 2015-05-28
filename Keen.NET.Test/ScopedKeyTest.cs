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
            const string plainText = "{\"filters\": [{\"property_name\": \"vendor_id\",\"operator\": \"eq\",\"property_value\": \"abc\"}],\"allowed_operations\": [ \"write\" ]}";
            var testKey = SettingsEnv.MasterKey;
            var cryptText = SettingsEnv.ReadKey;

            if (UseMocks)
            {
                cryptText =
                    "230C285D71306E362FC5A11BCD068405C5FDA9A52015FE770AB909B327A16AC4F1725A22C373CF2314A5E04643C283522E4D561A3DD9415306B563FC90F7C1EC7FE2E84E9866B3DA9627DE6284D0088A7B196523DEDC4F5A9D0EEFFEB18CFF7C52B75A35448A7CB06EE8523FF2DB9843538EBA64FF88A227CD881C0A3AE41613EE25D5CEA8124B59C88C390BA5234D65";
                testKey = "0123456789ABCDEF"; // ensure the key matches what cryptText was encrypted with
            }
            var decrypted = ScopedKey.Decrypt(testKey, cryptText);
            if (UseMocks)
                Assert.True(decrypted.Equals(plainText));
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

            Trace.WriteLine("plaintext: " + str);
            var scopedKey = ScopedKey.EncryptString(SettingsEnv.MasterKey, str, iv );
            Trace.WriteLine("encrypted: " + scopedKey);
            var decrypted = ScopedKey.Decrypt(SettingsEnv.MasterKey, scopedKey);
            Trace.WriteLine("decrypted: " + decrypted);
        }
    }
}

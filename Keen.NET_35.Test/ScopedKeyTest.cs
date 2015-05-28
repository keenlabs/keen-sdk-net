using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Diagnostics;


namespace Keen.NET_35.Test
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

                const string str = "{\"filters\": [{\"property_name\": \"account_id\",\"operator\": \"eq\",\"property_value\": 123}],\"allowed_operations\": [ \"read\" ]}";
                var secOps = JObject.Parse(str);
                var scopedKey = ScopedKey.Encrypt(settings.MasterKey, secOps);
            });
        }

        [Test]
        public void RoundTrip_PopulatedObject_Success()
        {
            var settings = new ProjectSettingsProviderEnv();

            const string str = "{\"filters\": [{\"property_name\": \"account_id\",\"operator\": \"eq\",\"property_value\": 123}],\"allowed_operations\": [ \"read\" ]}";
            var secOps = JObject.Parse(str);

            Assert.DoesNotThrow(() =>
            {
                var scopedKey = ScopedKey.Encrypt(settings.MasterKey, secOps);
                Console.WriteLine(scopedKey);
                var decrypted = ScopedKey.Decrypt(settings.MasterKey, scopedKey);
                var secOpsOut = JObject.Parse(decrypted); 
                Assert.True((string)secOps["allowed_operations"].First() == (string)(secOpsOut["allowed_operations"].First()));
            });
        }

        [Test]
        public void RoundTrip_PopulatedObject_WithIV_Success()
        {
            var settings = new ProjectSettingsProviderEnv();
            const string IV = "C0FFEEC0FFEEC0FFEEC0FFEEC0FFEEC0";

            const string str = "{\"filters\": [{\"property_name\": \"account_id\",\"operator\": \"eq\",\"property_value\": 123}],\"allowed_operations\": [ \"read\" ]}";
            var secOps = JObject.Parse(str);
            
            Assert.DoesNotThrow(() =>
            {
                var scopedKey = ScopedKey.Encrypt(settings.MasterKey, secOps, IV);
                var decrypted = ScopedKey.Decrypt(settings.MasterKey, scopedKey);
                var secOpsOut = JObject.Parse(decrypted);
                Assert.True((string)secOps["allowed_operations"].First() == (string)(secOpsOut["allowed_operations"].First()));
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
            const string vendor_guid = "abc";
            const bool isRead = false;

            var str = "{\"filters\": [{\"property_name\": \"vendor_id\",\"operator\": \"eq\",\"property_value\": \"VENDOR_GUID\"}],\"allowed_operations\": [ \"READ_OR_WRITE\" ]}";

            str = str.Replace("VENDOR_GUID", vendor_guid);

            if (isRead) str = str.Replace("READ_OR_WRITE", "read");
            else str = str.Replace("READ_OR_WRITE", "write");

            var rnd = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] bytes = new byte[16];
            rnd.GetBytes(bytes);
            
            var IV = String.Concat(bytes.Select(b => b.ToString("X2"))); Trace.WriteLine("IV: " + IV);

            var scopedKey = ScopedKey.EncryptString(SettingsEnv.MasterKey, str, IV );//System.Text.Encoding.Default.GetString(bytes));
            var decrypted = ScopedKey.Decrypt(SettingsEnv.MasterKey, scopedKey);
            Trace.WriteLine("decrypted: " + decrypted);
        }
    }
}

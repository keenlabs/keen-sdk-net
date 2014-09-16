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

            var settings = new ProjectSettingsProvider(SettingsEnv.ProjectId, writeKey: scopedKey);
            var client = new KeenClient(settings);
            client.AddEvent("X", new { vendor_id = "abc", X = "123" });
        }
    }
}

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;


namespace Keen.NetStandard.Test
{
    [TestFixture]
    public class ScopedKeyTest : TestBase
    {
        [Test]
        public void Encrypt_32CharKey_Success()
        {
            Assert.DoesNotThrow(() => ScopedKey.Encrypt("0123456789abcdef0123456789abcdef", null));
        }

        [Test]
        public void Encrypt_64CharKey_Success()
        {
            Assert.DoesNotThrow(() => ScopedKey.Encrypt("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", null));
        }

        [Test]
        public void Encrypt_32CharKeyWithFilter_Success()
        {
            var settings = new ProjectSettingsProvider("projId", "0123456789abcdef0123456789abcdef");

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
        public void Encrypt_64CharKeyWithFilter_Success()
        {
            var settings = new ProjectSettingsProvider("projId", "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");

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
        public void Encrypt_NullObject_Success()
        {
            Assert.DoesNotThrow(() => ScopedKey.Encrypt("0123456789ABCDEF0123456789ABCDEF", null));
        }

        [Test]
        public void Encrypt_NullKey_Throws()
        {
            Assert.Throws<KeenException>(() => ScopedKey.Encrypt(null, new { X = "X" }));
        }

        public void Encrypt_BlankKey_Throws()
        {
            Assert.Throws<KeenException>(() => ScopedKey.Encrypt("", new { X = "X" }));
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
                    "BAA51D1D03D49C1159E7298762AAC26493B20F579988E1EDA4613305F08E01CB702886F0FCB5312E5E18C6315A8049700816CA35BD952C75EB694AAA4A95535EE13CD9D5D8C97A215B4790638EA1DA3DB9484A0133D5289E2A22D5C2952E1F708540722EA832B093E147495A70ADF534242E961FDE3F0275E20D58F22B23F4BAE2A61518CB943818ABEF547DD68F68FE";
                testKey = "0123456789ABCDEF0123456789ABCDEF"; // ensure the key matches what cryptText was encrypted with
            }

            var decrypted = ScopedKey.Decrypt(testKey, cryptText);
            if (UseMocks)
                Assert.True(decrypted.Equals(plainText));
            else
                Assert.That(decrypted.IndexOf("timestamp") > 0);
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
            var scopedKey = ScopedKey.EncryptString(SettingsEnv.MasterKey, str, iv);
            Trace.WriteLine("encrypted: " + scopedKey);
            var decrypted = ScopedKey.Decrypt(SettingsEnv.MasterKey, scopedKey);
            Trace.WriteLine("decrypted: " + decrypted);

            // Make sure the input string exactly matches the decrypted string. This input isn't of
            // a length that is a multiple of block size or key size, so this would have required
            // manual padding in the past. The decrypted string shouldn't have any padding now.
            Assert.AreEqual(str, decrypted);
        }
    }
}

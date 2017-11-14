using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Keen.Core
{
    /// <summary>
    /// ScopedKey provides encryption and decryption functions which can be used to create 
    /// and read scoped keys, such as the API Read and Write keys. 
    /// </summary>
    public static class ScopedKey
    {
        private static readonly int KeySizeShort = 32;
        private static readonly int KeySizeLong = 64;
        private static readonly int IVHexSize = 32;


        /// <summary>
        /// Encrypt an object containing security options to create a scoped key.
        /// </summary>
        /// <param name="apiKey">Master API key</param>
        /// <param name="secOptions">An object that can be serialized to produce JSON formatted Security Options</param>
        /// <param name="IV">Optional IV, normally not required</param>
        /// <returns>Hex-encoded scoped key</returns>
        public static string Encrypt(string apiKey, object secOptions, string IV = null)
        {
            var secOptionsJson = JObject.FromObject(secOptions ?? new object()).ToString(Formatting.None);

            return EncryptString(apiKey, secOptionsJson, IV);
        }

        /// <summary>
        /// Encrypt a string containing JSON formatted Security Options to create a scoped key.
        /// </summary>
        /// <param name="apiKey">Master API key</param>
        /// <param name="secOptions">Security Options in JSON format</param>
        /// <param name="IV">Optional IV, normally not required</param>
        /// <returns>Hex-encoded scoped key</returns>
        public static string EncryptString(string apiKey, string secOptions, string IV = null)
        {
            try
            {
                if (!(null == IV || IV.Length == IVHexSize))
                    throw new KeenException(string.Format("Hex-encoded IV must be exactly {0} bytes, got {1}", IVHexSize, IV.Length));

                IV = IV ?? "";
                secOptions = secOptions ?? "";

                using (var aesAlg = GetAes(ConvertKey(apiKey), IV))
                using (var encryptor = aesAlg.CreateEncryptor())
                using (var msCrypt = new MemoryStream())
                {
                    using (var csCrypt = new CryptoStream(msCrypt, encryptor, CryptoStreamMode.Write))
                    using (var swCrypt = new StreamWriter(csCrypt))
                        swCrypt.Write(secOptions);

                    return ByteToHex(aesAlg.IV) + ByteToHex(msCrypt.ToArray());
                }
            }
            catch (Exception e)
            {
                throw new KeenException("Encryption error", e);
            }
        }

        /// <summary>
        /// Decrypt an existing scoped key.
        /// </summary>
        /// <param name="apiKey">Master API key</param>
        /// <param name="scopedKey">Scoped key to be decrypted</param>
        /// <returns>JSON formatted Security Options</returns>
        public static string Decrypt(string apiKey, string scopedKey)
        {
            try
            {
                scopedKey = scopedKey ?? "";

                // The IV is stored at the front of the string
                var IV = scopedKey.Substring(0, IVHexSize);

                // Encrypted data is stored after the IV part of the key
                var cryptHex = scopedKey.Substring(IVHexSize, scopedKey.Length - IVHexSize);

                using (var aesAlg = GetAes(ConvertKey(apiKey), IV))
                using (var decryptor = aesAlg.CreateDecryptor())
                using (var msCrypt = new MemoryStream(HexToByte(cryptHex)))
                using (var csCrypt = new CryptoStream(msCrypt, decryptor, CryptoStreamMode.Read))
                using (var srCrypt = new StreamReader(csCrypt))
                    return srCrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw new KeenException("Decryption error", ex);
            }
        }

        /// <summary>
        /// Convert an apiKey string to a byte array.
        /// </summary>
        /// <param name="apiKey">The key used to encrypt.</param>
        /// <returns>A byte array of the correct size and format for the key length.</returns>
        private static byte[] ConvertKey(string apiKey)
        {
            // Validate that the key matches the expected length of a Keen API Master key.
            if (string.IsNullOrWhiteSpace(apiKey) || ((apiKey.Length != KeySizeShort) && (apiKey.Length != KeySizeLong)))
                throw new KeenException(string.Format("Keen API Key must be either 32 or 64 characters"));

            byte[] aesKey;
            if (apiKey.Length == KeySizeLong)
                aesKey = HexToByte(apiKey);
            else
                aesKey = Encoding.UTF8.GetBytes(apiKey);

            return aesKey;
        }

        /// <summary>
        /// Set up an Aes instance with the correct mode, key and IV
        /// </summary>
        /// <param name="Key">Encryption key</param>
        /// <param name="IV">Initialization Vector, if left blank one will be generated.</param>
        /// <returns>An AES instance appropriate for encrypting with this key and IV.</returns>
        private static Aes GetAes(byte[] Key, string IV)
        {
            var aesAlg = AesCryptoServiceProvider.Create();
            aesAlg.KeySize = KeySizeShort * 8; // key size in bits
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;
            aesAlg.Key = Key;

            if (!string.IsNullOrEmpty(IV))
                aesAlg.IV = HexToByte(IV);

            return aesAlg;
        }

        private static string ByteToHex(byte[] a)
        {
            return String.Concat(a.Select(b => b.ToString("X2")).ToArray());
        }

        private static byte[] HexToByte(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("Hex string must have an even number of characters");

            Func<int, int> hexMap = (h) => h - (h < 58 ? 48 : (h < 97 ? 55 : 87));

            var result = new byte[hex.Length >> 1];
            for (int i = 0; i < (hex.Length >> 1); ++i)
                result[i] = (byte)((hexMap(hex[i << 1]) << 4) + (hexMap(hex[(i << 1) + 1])));

            return result;
        }
    }
}

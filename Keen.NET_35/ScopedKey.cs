using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Keen.NET_35
{
    /// <summary>
    /// ScopedKey provides encryption and decryption functions which can be used to create 
    /// and read scoped keys, such as the API Read and Write keys. 
    /// </summary>
    public static class ScopedKey
    {
        private static readonly int KeySize = 32;
        private static readonly int IVHexSize = 32;

        /// <summary>
        /// Encrypt an object containing security options to create a scoped key.
        /// </summary>
        /// <param name="apiKey">Master API key</param>
        /// <param name="secOptions">An object that can be serialized to produce JSON formatted Security Options</param>
        /// <param name="IV">Optional IV, normally not required</param>
        /// <returns></returns>
        public static string Encrypt(string apiKey, object secOptions, string IV = "")
        {
            var secOptionsJson = JObject.FromObject(secOptions ?? new object()).ToString();

            return EncryptString( apiKey, secOptionsJson, IV);
        }

        /// <summary>
        /// Encrypt a string containing JSON formatted Security Options to create a scoped key.
        /// </summary>
        /// <param name="apiKey">Master API key</param>
        /// <param name="secOptions">Security Options in JSON format</param>
        /// <param name="IV">Optional IV, normally not required</param>
        /// <returns>Hex-encoded scoped key</returns>
        public static string EncryptString(string apiKey, string secOptions, string IV = "")
        {
            try
            {
                if (!(IV.Length == 0 || IV.Length == IVHexSize))
                    throw new KeenException(string.Format("Hex-encoded IV must be exactly {0} bytes, got {1}", IVHexSize, IV.Length));

                secOptions = secOptions ?? "";
                apiKey = apiKey ?? "";

                // Pad the plaintext to a multiple of the key size
                var padSize = KeySize - (secOptions.Length % KeySize);
                secOptions = secOptions + Encoding.UTF8.GetString(Enumerable.Repeat((byte)padSize, padSize).ToArray());

                using (var aesAlg = GetAes(apiKey, IV))
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
                apiKey = apiKey ?? "";
                // The IV is stored at the front of the string
                var IV = scopedKey.Substring(0, IVHexSize);

                // Encrypted data is stored after the IV part of the key
                var cryptHex = scopedKey.Substring(IVHexSize, scopedKey.Length - IVHexSize);

                using (var aesAlg = GetAes(apiKey, IV))
                using (var decryptor = aesAlg.CreateDecryptor())
                using (var msCrypt = new MemoryStream(HexToByte(cryptHex)))
                using (var csCrypt = new CryptoStream(msCrypt, decryptor, CryptoStreamMode.Read))
                using (var srCrypt = new StreamReader(csCrypt))
                    return RemovePadding(srCrypt.ReadToEnd());
            }
            catch (Exception ex)
            {
                throw new KeenException("Decryption error" + ex.Message, ex);
            }
        }

        /// <summary>
        /// Set up an Aes instance with the correct mode, key and IV
        /// </summary>
        /// <param name="Key">Encryption key</param>
        /// <param name="IV">Initialization Vector, if left blank one will be generated.</param>
        /// <returns></returns>
        private static Aes GetAes(string Key, string IV)
        {
            var aesAlg = AesCryptoServiceProvider.Create();
            aesAlg.KeySize = KeySize * 8; // key size in bits
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.None;
            aesAlg.Key = Encoding.UTF8.GetBytes(AddPadding(Key));
            if (IV != "")
                aesAlg.IV = HexToByte(IV);
            return aesAlg;
        }

        /// <summary>
        /// Pad the key to the required size using PKCS7 padding
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string AddPadding(string text)
        {
            var padSize = KeySize - text.Length;
            return text + new String(Convert.ToChar(padSize), padSize);
        }

        /// <summary>
        /// Remove PKCS5/7 padding
        /// </summary>
        private static string RemovePadding(string text)
        {
            byte padSize = Convert.ToByte(text.Last());
            if (padSize <= KeySize)
                text = text.Substring(0, text.Length - padSize);
            return text;
        }

        private static string ByteToHex(byte[] a)
        {
            return String.Concat(a.Select(b => b.ToString("X2")).ToArray());
        }

        private static byte[] HexToByte(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("Hex string must have an even number of characters");

            Func<int,int> hexMap = (h) => h - (h < 58 ? 48 : (h < 97 ? 55 : 87));

            var result = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
                result[i] = (byte)((hexMap(hex[i << 1]) << 4) + (hexMap(hex[(i << 1) + 1])));

            return result;
        }

    }
}

using System;
using System.Text;
using System.Security.Cryptography;

namespace ODOLauncher.Models
{
    public static class Encoder
    {
        private const string MagicKey = "DjC9BwHpJ3vpzifnFoha";

        public static string Encode(string str)
        {
            var b = Encoding.UTF8.GetBytes(str);
            var cryptoServiceProvider = new MD5CryptoServiceProvider();
            var arrayHash = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(Encoder.MagicKey));
            cryptoServiceProvider.Clear();
            var cryptoServiceProvider1 = new TripleDESCryptoServiceProvider
            {
                Key = arrayHash, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7
            };
            var bytes = cryptoServiceProvider1.CreateEncryptor().TransformFinalBlock(b, 0, b.Length);
            cryptoServiceProvider1.Clear();
            return Convert.ToBase64String(bytes, 0, bytes.Length);
        }

        public static string Decode(string str)
        {
            var b = Convert.FromBase64String(str);
            var cryptoServiceProvider = new MD5CryptoServiceProvider();
            var arrayHash = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(Encoder.MagicKey));
            cryptoServiceProvider.Clear();
            var cryptoServiceProvider1 = new TripleDESCryptoServiceProvider
            {
                Key = arrayHash, Mode = CipherMode.ECB, Padding = PaddingMode.PKCS7
            };
            var bytes = cryptoServiceProvider1.CreateDecryptor().TransformFinalBlock(b, 0, b.Length);
            cryptoServiceProvider1.Clear();
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
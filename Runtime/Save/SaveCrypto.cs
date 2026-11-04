using System;
using System.Security.Cryptography;
using System.Text;

namespace RippleGame.Save
{
    /// <summary>AES-256-CBC with encrypt-then-MAC: "RGSV" | version | IV | ciphertext | HMAC-SHA256.</summary>
    /// <remarks>Crypto types are constructed directly so IL2CPP stripping cannot remove a reflection-resolved implementation.</remarks>
    internal static class SaveCrypto
    {
        internal const int KeySize = 32;
        private const byte FormatVersion = 1;
        private const int IvSize = 16;
        private const int BlockSize = 16;
        private const int MacSize = 32;
        private static readonly byte[] Magic = { (byte)'R', (byte)'G', (byte)'S', (byte)'V' };
        private static readonly int HeaderSize = Magic.Length + 1;
        private static readonly byte[] EncryptionLabel = Encoding.UTF8.GetBytes("ripplegame.save.encryption");
        private static readonly byte[] AuthenticationLabel = Encoding.UTF8.GetBytes("ripplegame.save.authentication");

        internal static byte[] DecodeKey(string base64Key)
        {
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentException("A save key is required.", nameof(base64Key));

            byte[] key;
            try
            {
                key = Convert.FromBase64String(base64Key);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("The save key must be Base64 text.", nameof(base64Key), exception);
            }

            if (key.Length != KeySize) throw new ArgumentException($"The save key must decode to {KeySize} bytes.", nameof(base64Key));
            return key;
        }

        internal static string GenerateKey()
        {
            var key = new byte[KeySize];
            using (var random = new RNGCryptoServiceProvider()) random.GetBytes(key);
            return Convert.ToBase64String(key);
        }

        internal static byte[] Seal(byte[] plain, byte[] masterKey)
        {
            var iv = new byte[IvSize];
            using (var random = new RNGCryptoServiceProvider()) random.GetBytes(iv);

            byte[] cipher;
            using (var aes = CreateAes(masterKey, iv))
            using (var encryptor = aes.CreateEncryptor())
                cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            var output = new byte[HeaderSize + IvSize + cipher.Length + MacSize];
            Buffer.BlockCopy(Magic, 0, output, 0, Magic.Length);
            output[Magic.Length] = FormatVersion;
            Buffer.BlockCopy(iv, 0, output, HeaderSize, IvSize);
            Buffer.BlockCopy(cipher, 0, output, HeaderSize + IvSize, cipher.Length);

            var macStart = output.Length - MacSize;
            var mac = ComputeMac(output, macStart, masterKey);
            Buffer.BlockCopy(mac, 0, output, macStart, MacSize);
            return output;
        }

        internal static bool TryOpen(byte[] data, byte[] masterKey, out byte[] plain)
        {
            plain = null;
            if (data == null || data.Length < HeaderSize + IvSize + BlockSize + MacSize) return false;

            for (var i = 0; i < Magic.Length; i++)
                if (data[i] != Magic[i]) return false;
            if (data[Magic.Length] != FormatVersion) return false;

            var macStart = data.Length - MacSize;
            if (!MacMatches(ComputeMac(data, macStart, masterKey), data, macStart)) return false;

            var iv = new byte[IvSize];
            Buffer.BlockCopy(data, HeaderSize, iv, 0, IvSize);
            try
            {
                using (var aes = CreateAes(masterKey, iv))
                using (var decryptor = aes.CreateDecryptor())
                    plain = decryptor.TransformFinalBlock(data, HeaderSize + IvSize, macStart - HeaderSize - IvSize);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static SymmetricAlgorithm CreateAes(byte[] masterKey, byte[] iv)
        {
            return new AesManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = KeySize * 8,
                Key = Derive(masterKey, EncryptionLabel),
                IV = iv
            };
        }

        private static byte[] ComputeMac(byte[] data, int count, byte[] masterKey)
        {
            using (var hmac = new HMACSHA256(Derive(masterKey, AuthenticationLabel)))
                return hmac.ComputeHash(data, 0, count);
        }

        private static byte[] Derive(byte[] masterKey, byte[] label)
        {
            using (var hmac = new HMACSHA256(masterKey))
                return hmac.ComputeHash(label);
        }

        private static bool MacMatches(byte[] expected, byte[] data, int offset)
        {
            var difference = 0;
            for (var i = 0; i < MacSize; i++)
                difference |= expected[i] ^ data[offset + i];
            return difference == 0;
        }
    }
}

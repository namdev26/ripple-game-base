using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RippleGame.Sdk.Editor
{
    internal static class SdkHash
    {
        /// <summary>Lowercase hex SHA-256 of the file's bytes.</summary>
        public static string Sha256Hex(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}

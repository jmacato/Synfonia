using System.Security.Cryptography;
using System.Text;

namespace Synfonia.Backend
{
    public class CryptoMethods
    {
        public static string ComputeSha256Hash(byte[] rawData)
        {
            // Create a SHA256   
            using SHA256 sha256Hash = SHA256.Create();

            // ComputeHash - returns byte array  
            byte[] bytes = sha256Hash.ComputeHash(rawData);

            // Convert byte array to a string   
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
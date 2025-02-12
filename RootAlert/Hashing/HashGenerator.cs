using System.Security.Cryptography;
using System.Text;

namespace RootAlert.Hashing
{
    internal static class HashGenerator
    {
        internal static async Task<string> GenerateErrorHash(Exception exception)
        {
            using var sha256 = SHA256.Create();
            string rawData = exception.Message + exception.StackTrace;
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rawData));
            byte[] bytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToBase64String(bytes);
        }
    }
}

using System.Security.Cryptography;
using System.Text;

namespace Sherlog.Hashing;

internal static class HashGenerator
{
    internal static string GenerateErrorHash(Exception exception)
    {
        using var sha256 = SHA256.Create();
        string rawData = exception.Message + exception.StackTrace;
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToBase64String(bytes);
    }
}

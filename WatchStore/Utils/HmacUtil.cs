using System.Security.Cryptography;
using System.Text;

namespace WatchStore.Utils
{
    public static class HmacUtil
    {
        public static string HmacSHA512(string key, string data)
        {
            var k = Encoding.UTF8.GetBytes(key);
            var bytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(k);
            var hash = hmac.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}

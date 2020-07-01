using System.Security.Cryptography;
using System.Text;

namespace Michaelsoft.Mailer.Extensions
{
    public static class StringHelper
    {

        public static string Capitalize(this string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string Sha1(this string s)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString().Substring(0, 40);
            }
        }

        public static bool IsNullOrEmpty(this string s)
        {
            return s == null || s.Trim().Equals("");
        }

    }
}
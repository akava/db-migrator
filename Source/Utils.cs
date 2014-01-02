using System.Security.Cryptography;
using System.Text;

namespace DbMigrator
{
    public class Utils
    {
        public static string ComputeMd5Hash(string input)
        {
            using (var md5Hash = MD5.Create())
            {
                var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                var hash = new StringBuilder();
                foreach (var b in data)
                    hash.Append(b.ToString("x2"));

                return hash.ToString();
            }
        } 
    }
}

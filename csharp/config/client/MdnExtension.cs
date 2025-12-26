using System.Security.Cryptography;
using System.Text;

namespace Health.Direct.Config.Client.MonitorService
{
    public static class MdnExtension
    {
        public static string GetMdnIdentifier(this Mdn mdn)
        {
            if (mdn.MessageId == null || mdn.Recipient == null)
            {
                return null;
            }
            var fieldsSb = new StringBuilder();
            fieldsSb.Append(mdn.MessageId.ToLower()).Append(mdn.Recipient.ToLower()).Append(mdn.Status.ToLower());

            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(fieldsSb.ToString());
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
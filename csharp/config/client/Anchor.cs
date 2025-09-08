using System.Security.Cryptography.X509Certificates;
using Health.Direct.Common.Certificates;
using Health.Direct.Common.Extensions;

namespace Health.Direct.Config.Client.CertificateService
{
    public partial class Anchor
    {
        public bool HasData => (this.Data != null && this.Data.Length > 0);

        public DisposableX509Certificate2 ToX509Certificate()
        {
            return new DisposableX509Certificate2(this.Data, string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }

        public static X509Certificate2Collection ToX509Collection(Anchor[] source)
        {
            if (source.IsNullOrEmpty())
            {
                return null;
            }

            X509Certificate2Collection x509Coll = new X509Certificate2Collection();
            if (source != null)
            {
                for (int i = 0; i < source.Length; ++i)
                {
                    x509Coll.Add(source[i].ToX509Certificate());
                }
            }
            return x509Coll;
        }
    }
}
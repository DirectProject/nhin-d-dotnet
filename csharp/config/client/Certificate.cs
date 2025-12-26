using Health.Direct.Common;
using Health.Direct.Common.Certificates;
using Health.Direct.Common.Extensions;
using System.Security.Cryptography.X509Certificates;


namespace Health.Direct.Config.Client.CertificateService
{
    public partial class Certificate
    {
        public Certificate(string owner, byte[] sourceFileBytes, string password)
            : this(owner, Import(sourceFileBytes, password))
        {
        }

        public Certificate(string owner, X509Certificate2 certificate)
            : this(owner, certificate, true)
        {
        }

        public Certificate(string owner, X509Certificate2 certificate, bool includePrivateKey)
        {
            this.Owner = owner;
            this.SetX509Certificate(certificate, includePrivateKey);
        }

        public bool HasData => (this.Data != null && this.Data.Length > 0);

        void SetX509Certificate(X509Certificate2 certificate, bool includePrivateKey)
        {
            this.Thumbprint = certificate.Thumbprint;
            this.Data = includePrivateKey ? certificate.Export(X509ContentType.Pfx) : certificate.Export(X509ContentType.Cert);
            this.CreateDate = DateTimeHelper.Now;
            this.ValidStartDate = certificate.NotBefore;
            this.ValidEndDate = certificate.NotAfter;
        }

        internal static X509Certificate2 Import(byte[] sourceFileBytes, string password)
        {
            return new X509Certificate2(sourceFileBytes, password, X509KeyStorageFlags.Exportable);
        }

        public DisposableX509Certificate2 ToX509Certificate()
        {
            return new DisposableX509Certificate2(this.Data, string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }

        public static X509Certificate2Collection ToX509Collection(Certificate[] source)
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
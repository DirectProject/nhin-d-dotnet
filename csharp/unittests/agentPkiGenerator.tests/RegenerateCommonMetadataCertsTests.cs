using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Health.Direct.Agent.PkiGenerator.Tests
{   
    public class RegenerateCommonMetadataCertsTests
    {
        [Fact]
        public void RegenerateLocalP12Files()
        {
            // Create .p12 files locally under test project folder
            var outputDir = Path.Combine("common.metadata", "domains", "certs");
            Directory.CreateDirectory(outputDir);

            // Generate a small set of test certs with predictable names
            var names = new List<string>();

            for (int index = 1; index <= 10; index++)
            {
                for (int iteration = 1; iteration <= 3; iteration++)
                {
                    names.Add($"domain{index}.test.com.{iteration}");
                }
            }
            
            foreach (var cn in names)
            {
                var cert = CreateSelfSigned(cn);
                const string password = "Passw0rd!";

                // .p12 is the same format as .pfx in Windows/.NET
                var p12Bytes = cert.Export(X509ContentType.Pfx, password);
                var filePath = Path.Combine(outputDir, $"{cn}.pfx");
                File.WriteAllBytes(filePath, p12Bytes);
            }
            
        }

        private static X509Certificate2 CreateSelfSigned(string commonName)
        {
            using var rsa = RSA.Create(2048);
            var subject = $"CN={commonName}";

            var req = new CertificateRequest(
                subject,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // End-entity Basic Constraints
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

            // Key Usage
            var keyUsage = X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment;
            req.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));

            // EKU: Secure Email
            var eku = new OidCollection { new("1.3.6.1.5.5.7.3.4") };
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));

            // Subject Key Identifier
            req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

            var now = DateTimeOffset.UtcNow;
            using var cert = req.CreateSelfSigned(now.AddMinutes(-5), now.AddYears(5));

            return new X509Certificate2(cert.Export(X509ContentType.Pfx), (string)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }
    }
}

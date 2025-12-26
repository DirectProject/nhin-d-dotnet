using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Health.Direct.Agent.Tests
{
    public class CreateHoboCertificateTests
    {
        [Fact]
        public void CreateHoboAnchorAndIssuedCert()
        {
            // Folder layout mimics existing tests
            var anchorDirs = new[]
            {
                "NewCertificates/hobo/IncomingAnchors"
            };

            var publicDirs = new[]
            {
                "NewCertificates/hobo/Public"
            };

            var privateDirs = new[]
            {
                "NewCertificates/hobo/Private"
            };

            // 1) Create the self-signed anchor (CA)
            CreateCAs(
                fileBaseName: "NoAnchor.Hobo.Lab",
                subject: "CN=NoAnchor.Hobo.Lab",
                cerOutputDirs: anchorDirs);

            // 2) Issue an end-entity cert from the CA
            CreateEndCert(
                caFileBaseName: "NewCertificates/hobo/IncomingAnchors/NoAnchor.Hobo.Lab",
                cerOutputDirs: publicDirs,
                pfxOutputDirs: privateDirs,
                endFileBaseName: "Direct.NoAnchor.Hobo.Lab",
                subject: "CN=Direct.NoAnchor.Hobo.Lab");
        }

        private void CreateEndCert(
            string caFileBaseName,
            IEnumerable<string>? cerOutputDirs,
            IEnumerable<string>? pfxOutputDirs,
            string endFileBaseName,
            string subject,
            int yearsValid = 5,
            string? rfc822Email = null)
        {
            if (string.IsNullOrWhiteSpace(caFileBaseName)) throw new ArgumentException("CA file base name required", nameof(caFileBaseName));
            if (string.IsNullOrWhiteSpace(endFileBaseName)) throw new ArgumentException("End certificate file base name required", nameof(endFileBaseName));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject required", nameof(subject));

            var caPfxPath = $"{caFileBaseName}.pfx";
            Assert.True(File.Exists(caPfxPath), $"CA PFX not found: {caPfxPath}. Run anchor creation first.");

            var caCert = new X509Certificate2(File.ReadAllBytes(caPfxPath), (string?)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            Assert.True(caCert.HasPrivateKey, "CA certificate must have a private key.");

            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    subject,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // End-entity constraints
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

                var keyUsageFlags = X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment;
                request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsageFlags, true));

                // Secure Email EKU
                var eku = new OidCollection { new Oid("1.3.6.1.5.5.7.3.4") };
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));

                // Optional SAN: RFC822 email
                if (!string.IsNullOrWhiteSpace(rfc822Email))
                {
                    var sanBuilder = new SubjectAlternativeNameBuilder();
                    sanBuilder.AddEmailAddress(rfc822Email);
                    request.CertificateExtensions.Add(sanBuilder.Build());
                }

                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // AKI from CA SKI (ensure CA has one; if not create synthetic)
                var caSki = caCert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().FirstOrDefault()
                            ?? new X509SubjectKeyIdentifierExtension(caCert.PublicKey, false);
                request.CertificateExtensions.Add(BuildAuthorityKeyIdentifierFromSubjectKeyId(caSki));

                var now = DateTimeOffset.UtcNow;
                using (var issued = request.Create(
                           caCert,
                           now,
                           now.AddYears(yearsValid),
                           GenerateSerial()))
                {
                    var endWithKey = issued.CopyWithPrivateKey(rsa);
                    const string pfxPassword = "Passw0rd!";
                    var pfxBytes = endWithKey.Export(X509ContentType.Pfx, pfxPassword);

                    var verify = new X509Certificate2(
                        pfxBytes,
                        pfxPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
                    if (!verify.HasPrivateKey)
                    {
                        throw new InvalidOperationException("PFX verification failed: private key missing.");
                    }

                    var cerBytes = endWithKey.Export(X509ContentType.Cert);

                    if (cerOutputDirs != null)
                    {
                        foreach (var dir in cerOutputDirs)
                        {
                            if (string.IsNullOrWhiteSpace(dir)) continue;
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            File.WriteAllBytes(Path.Combine(dir, $"{endFileBaseName}.cer"), cerBytes);
                        }
                    }

                    if (pfxOutputDirs != null)
                    {
                        foreach (var dir in pfxOutputDirs)
                        {
                            if (string.IsNullOrWhiteSpace(dir)) continue;
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            File.WriteAllBytes(Path.Combine(dir, $"{endFileBaseName}.pfx"), pfxBytes);
                        }
                    }
                }
            }
        }

        private void CreateCAs(
            string fileBaseName,
            string subject,
            IEnumerable<string>? cerOutputDirs = null)
        {
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject must be provided.", nameof(subject));
            if (string.IsNullOrWhiteSpace(fileBaseName)) throw new ArgumentException("Filename base must be provided.", nameof(fileBaseName));

            var now = DateTimeOffset.UtcNow;
            var recreated = CreateSelfSignedAnchor(
                subject,
                now,
                now.AddYears(20));

            Assert.Equal(subject, recreated.Subject);
            Assert.Equal(recreated.Subject, recreated.Issuer);

            var cerBytes = recreated.Export(X509ContentType.Cert);
            var pfxBytes = recreated.Export(X509ContentType.Pfx);

            if (cerOutputDirs != null)
            {
                foreach (var dir in cerOutputDirs)
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllBytes(Path.Combine(dir, $"{fileBaseName}.cer"), cerBytes);
                    File.WriteAllBytes(Path.Combine(dir, $"{fileBaseName}.pfx"), pfxBytes);
                }
            }
        }

        private static X509Certificate2 CreateSelfSignedAnchor(string subject, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest(
                subject,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

            var keyUsageFlags = X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature;
            request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsageFlags, true));

            var ekuOids = new OidCollection();
            ekuOids.Add(new Oid("1.3.6.1.5.5.7.3.4"));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(ekuOids, false));

            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            var skiExt = request.CertificateExtensions.OfType<X509SubjectKeyIdentifierExtension>().First();
            var aki = BuildAuthorityKeyIdentifierFromSubjectKeyId(skiExt);
            request.CertificateExtensions.Add(aki);

            var cert = request.CreateSelfSigned(notBefore, notAfter);

            return new X509Certificate2(cert.Export(X509ContentType.Pfx), (string?)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        private static X509Extension BuildAuthorityKeyIdentifierFromSubjectKeyId(X509SubjectKeyIdentifierExtension ski)
        {
            // AuthorityKeyIdentifier ::= SEQUENCE { keyIdentifier [0] IMPLICIT OCTET STRING OPTIONAL }
            // Encoded: 30 16 80 14 <20 bytes>
            byte[] keyId = HexToBytes(ski.SubjectKeyIdentifier);
            if (keyId.Length != 20)
            {
                throw new InvalidOperationException("Unexpected SKI length for AKI construction.");
            }

            var akiBytes = new byte[2 + 2 + 20];
            akiBytes[0] = 0x30;
            akiBytes[1] = 0x16;
            akiBytes[2] = 0x80;
            akiBytes[3] = 0x14;
            Buffer.BlockCopy(keyId, 0, akiBytes, 4, 20);

            return new X509Extension("2.5.29.35", akiBytes, false);
        }

        private static byte[] HexToBytes(string hex)
        {
            if (hex == null) return Array.Empty<byte>();
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private static byte[] GenerateSerial()
        {
            var serial = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serial);
            }
            return serial;
        }
    }
}

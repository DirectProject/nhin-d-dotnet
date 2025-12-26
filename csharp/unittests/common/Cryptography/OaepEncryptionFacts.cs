/* 
 Copyright (c) 2025, Direct Project
 All rights reserved.

 Authors:
    GitHub Copilot
 
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Health.Direct.Common.Cryptography;
using Health.Direct.Common.Mail;
using Health.Direct.Common.Mime;
using Org.BouncyCastle.Asn1.Pkcs;
using Xunit;
using Xunit.Abstractions;

namespace Health.Direct.Common.Tests.Cryptography
{
    public class OaepEncryptionFacts : TestingBase
    {
        private readonly ITestOutputHelper _output;

        public OaepEncryptionFacts(ITestOutputHelper output)
        {
            _output = output;
        }

        private static X509Certificate2 GenerateSelfSignedEnciphermentCert(string subjectName)
        {
            using (var rsa = RSA.Create(2048))
            {
                var req = new CertificateRequest(
                    new X500DistinguishedName($"CN={subjectName}"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                var keyUsage = new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                    critical: true);
                req.CertificateExtensions.Add(keyUsage);

                var basicConstraints = new X509BasicConstraintsExtension(false, false, 0, true);
                req.CertificateExtensions.Add(basicConstraints);

                var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
                var notAfter = DateTimeOffset.UtcNow.AddYears(3);
                var cert = req.CreateSelfSigned(notBefore, notAfter);

                return cert;
            }
        }

        private static X509Certificate2 LoadRecipientCert()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var certPath = Path.Combine(baseDir, "DnsResolver", "DnsTestCerts", "umesh.cer");
            Assert.True(File.Exists(certPath), $"Test recipient certificate not found at {certPath}");
            return new X509Certificate2(certPath);
        }

        private static MimeEntity CreatePlainTextEntity(string text)
        {
            return new MimeEntity
            {
                ContentType = MimeStandard.MediaType.TextPlain,
                ContentTransferEncoding = TransferEncoding.SevenBit.AsString(),
                Body = new Body(text)
            };
        }

        [Fact]
        public void BcCryptographer_Encrypts_With_RSAES_OAEP()
        {
            var recipient = LoadRecipientCert();
            var entity = CreatePlainTextEntity("Hello OAEP");

            var crypt = new BcSMIMECryptographer();
            var encryptedEntity = crypt.Encrypt(entity, recipient);
            var encryptedBytes = crypt.GetEncryptedBytes(encryptedEntity);

            var cms = new EnvelopedCms();
            cms.Decode(encryptedBytes);
            Assert.True(cms.RecipientInfos.Count > 0, "No recipient infos present");

            var keyAlgOid = cms.RecipientInfos[0].KeyEncryptionAlgorithm.Oid.Value;
            _output.WriteLine($"BcSMIMECryptographer KeyEncryptionAlgorithm OID: {keyAlgOid}");
            _output.WriteLine($"Expected RSAES-OAEP OID: {PkcsObjectIdentifiers.IdRsaesOaep.Id}");
            Assert.Equal(PkcsObjectIdentifiers.IdRsaesOaep.Id, keyAlgOid);
        }

        [Fact]
        public void DefaultSmimeCryptographer_Reports_Key_Encryption_Algorithm()
        {
            // This test documents what algorithm the default SMIMECryptographer uses.
            // On modern .NET Framework 4.8 with recent Windows updates, EnvelopedCms may use OAEP.
            // The BcSMIMECryptographer explicitly uses OAEP regardless of Windows policy.
            var recipient = LoadRecipientCert();
            var entity = CreatePlainTextEntity("Hello Default");

            var crypt = SMIMECryptographer.Default;
            var encryptedEntity = crypt.Encrypt(entity, recipient);
            var encryptedBytes = crypt.GetEncryptedBytes(encryptedEntity);

            var cms = new EnvelopedCms();
            cms.Decode(encryptedBytes);
            Assert.True(cms.RecipientInfos.Count > 0, "No recipient infos present");

            var keyAlgOid = cms.RecipientInfos[0].KeyEncryptionAlgorithm.Oid.Value;
            _output.WriteLine($"SMIMECryptographer.Default KeyEncryptionAlgorithm OID: {keyAlgOid}");
            _output.WriteLine($"rsaEncryption (PKCS#1 v1.5) OID: {PkcsObjectIdentifiers.RsaEncryption.Id}");
            _output.WriteLine($"RSAES-OAEP OID: {PkcsObjectIdentifiers.IdRsaesOaep.Id}");
            Assert.NotNull(keyAlgOid);
        }

        [Fact]
        public void BcEncrypt_OAEP_SHA256_DefaultDecrypt_Roundtrip()
        {
            var recipient = GenerateSelfSignedEnciphermentCert("OAEP-SHA256-Roundtrip");
            var entity = CreatePlainTextEntity("Hello OAEP Roundtrip");

            var bc = new BcSMIMECryptographer();
            var encryptedEntity = bc.Encrypt(entity, recipient);
            var encryptedBytes = bc.GetEncryptedBytes(encryptedEntity);

            var cms = new EnvelopedCms();
            cms.Decode(encryptedBytes);
            Assert.True(cms.RecipientInfos.Count > 0, "No recipient infos present");
            var keyAlgOid = cms.RecipientInfos[0].KeyEncryptionAlgorithm.Oid.Value;
            _output.WriteLine($"Encrypted with Bc; KeyEncAlg OID: {keyAlgOid} (OAEP expected)");

            Exception ex = Record.Exception(() =>
            {
                var decrypted = SMIMECryptographer.Default.DecryptEntity(encryptedBytes, recipient);
                Assert.NotNull(decrypted);
                Assert.Equal("Hello OAEP Roundtrip", decrypted.Body.Text);
            });

            if (ex != null)
            {
                _output.WriteLine($"Default Decrypt failed: {ex.GetType().Name} - {ex.Message}");
                Assert.Fail("Default SMIMECryptographer could not decrypt OAEP-SHA256 wrapped content key.");
            }
            else
            {
                _output.WriteLine("Default Decrypt succeeded with OAEP-SHA256.");
            }
        }
    }
}

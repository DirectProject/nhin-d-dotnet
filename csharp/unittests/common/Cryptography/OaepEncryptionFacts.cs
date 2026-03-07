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
using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Health.Direct.Common.Cryptography;
using Health.Direct.Common.Mime;
using Org.BouncyCastle.Asn1.Nist;
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
        public void BcCryptographer_OAEP_Uses_SHA256_Params()
        {
            var recipient = GenerateSelfSignedEnciphermentCert("OAEP-SHA256-Params");
            var entity = CreatePlainTextEntity("Hello OAEP SHA256");

            var bc = new SMIMECryptographer();
            var encryptedEntity = bc.Encrypt(entity, recipient);
            var encryptedBytes = bc.GetEncryptedBytes(encryptedEntity);

            var asn1 = Org.BouncyCastle.Asn1.Asn1Object.FromByteArray(encryptedBytes);
            var contentInfo = Org.BouncyCastle.Asn1.Cms.ContentInfo.GetInstance(asn1);
            var envelopedData = Org.BouncyCastle.Asn1.Cms.EnvelopedData.GetInstance(contentInfo.Content);
            var recipientInfo = Org.BouncyCastle.Asn1.Cms.RecipientInfo.GetInstance(envelopedData.RecipientInfos[0]);
            var ktri = Org.BouncyCastle.Asn1.Cms.KeyTransRecipientInfo.GetInstance(recipientInfo.Info);
            var algId = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(ktri.KeyEncryptionAlgorithm);

            Assert.Equal(PkcsObjectIdentifiers.IdRsaesOaep.Id, algId.Algorithm.Id);

            var oaep = Org.BouncyCastle.Asn1.Pkcs.RsaesOaepParameters.GetInstance(algId.Parameters);
            var hashAlg = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(oaep.HashAlgorithm);
            var mgfAlg = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(oaep.MaskGenAlgorithm);

            _output.WriteLine($"Bc OAEP Hash: {hashAlg.Algorithm.Id}");
            _output.WriteLine($"Bc OAEP MGF: {mgfAlg.Algorithm.Id}");

            Assert.Equal(NistObjectIdentifiers.IdSha256.Id, hashAlg.Algorithm.Id);
            // MGF1 OID with SHA-256 parameter
            Assert.Equal(PkcsObjectIdentifiers.IdMgf1.Id, mgfAlg.Algorithm.Id);
            var mgfParam = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(mgfAlg.Parameters);
            Assert.Equal(NistObjectIdentifiers.IdSha256.Id, mgfParam.Algorithm.Id);
        }

        [Fact]
        public void DefaultSmimeCryptographer_OAEP_Uses_SHA1()
        {
            var recipient = GenerateSelfSignedEnciphermentCert("Default-OAEP-Params");
            var entity = CreatePlainTextEntity("Hello Default OAEP Params");

            var def = LegacySMIMECryptographer.Default;
            var encryptedEntity = def.Encrypt(entity, recipient);
            var encryptedBytes = def.GetEncryptedBytes(encryptedEntity);

            var asn1 = Org.BouncyCastle.Asn1.Asn1Object.FromByteArray(encryptedBytes);
            var contentInfo = Org.BouncyCastle.Asn1.Cms.ContentInfo.GetInstance(asn1);
            var envelopedData = Org.BouncyCastle.Asn1.Cms.EnvelopedData.GetInstance(contentInfo.Content);
            var recipientInfo = Org.BouncyCastle.Asn1.Cms.RecipientInfo.GetInstance(envelopedData.RecipientInfos[0]);
            var ktri = Org.BouncyCastle.Asn1.Cms.KeyTransRecipientInfo.GetInstance(recipientInfo.Info);
            var algId = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(ktri.KeyEncryptionAlgorithm);

            var keyAlgOid = algId.Algorithm.Id;
            _output.WriteLine($"Default KeyEncAlg OID: {keyAlgOid}");
            

            Assert.Equal(PkcsObjectIdentifiers.IdRsaesOaep.Id, keyAlgOid);
            var oaep = Org.BouncyCastle.Asn1.Pkcs.RsaesOaepParameters.GetInstance(algId.Parameters);
            var hashAlg = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(oaep.HashAlgorithm);
            var mgfAlg = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(oaep.MaskGenAlgorithm);

            _output.WriteLine($"Default OAEP Hash: {hashAlg.Algorithm.Id}");
            _output.WriteLine($"Default OAEP MGF: {mgfAlg.Algorithm.Id}");

            // Expect legacy SHA-1-based OAEP if using OAEP
            Assert.Equal("1.3.14.3.2.26", hashAlg.Algorithm.Id); // SHA-1
            Assert.Equal(PkcsObjectIdentifiers.IdMgf1.Id, mgfAlg.Algorithm.Id);
            var mgfParam = Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier.GetInstance(mgfAlg.Parameters);
            Assert.Equal("1.3.14.3.2.26", mgfParam.Algorithm.Id); // SHA-1
        }

        /// <summary>
        /// Encrypt with BouncyCastle OAEP-SHA256, decrypt with Default SMIMECryptographer
        /// </summary>
        [Fact]
        public void BcEncrypt_OAEP_SHA256_DefaultDecrypt_Roundtrip()
        {
            var recipient = GenerateSelfSignedEnciphermentCert("OAEP-SHA256-Roundtrip");
            var entity = CreatePlainTextEntity("Hello OAEP Roundtrip");

            var bc = new SMIMECryptographer();
            var encryptedEntity = bc.Encrypt(entity, recipient);
            var encryptedBytes = bc.GetEncryptedBytes(encryptedEntity);

            var cms = new EnvelopedCms();
            cms.Decode(encryptedBytes);
            Assert.True(cms.RecipientInfos.Count > 0, "No recipient infos present");
            var keyAlgOid = cms.RecipientInfos[0].KeyEncryptionAlgorithm.Oid.Value;
            _output.WriteLine($"Encrypted with Bc; KeyEncAlg OID: {keyAlgOid} (OAEP expected)");

            var ex = Record.Exception(() =>
            {
                var decrypted = LegacySMIMECryptographer.Default.DecryptEntity(encryptedBytes, recipient);
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

        /// <summary>
        /// Encrypt with Default SMIMECryptographer OAEP-SHA1, decrypt with BouncyCastle
        /// </summary>
        [Fact]
        public void SMIMECryptographer_OAEP_SHA1_DefaultDecrypt_BCSMIMECryptographer_Roundtrip()
        {
            var recipient = GenerateSelfSignedEnciphermentCert("OAEP-SHA256-Roundtrip");
            var entity = CreatePlainTextEntity("Hello OAEP Roundtrip");

            var sc = LegacySMIMECryptographer.Default;
            var encryptedEntity = sc.Encrypt(entity, recipient);
            var encryptedBytes = sc.GetEncryptedBytes(encryptedEntity);

            var cms = new EnvelopedCms();
            cms.Decode(encryptedBytes);
            Assert.True(cms.RecipientInfos.Count > 0, "No recipient infos present");
            var keyAlgOid = cms.RecipientInfos[0].KeyEncryptionAlgorithm.Oid.Value;
            _output.WriteLine($"Encrypted with Sc; KeyEncAlg OID: {keyAlgOid} (OAEP expected)");

            var ex = Record.Exception(() =>
            {
                var decrypted = new SMIMECryptographer().DecryptEntity(encryptedBytes, recipient);
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
                _output.WriteLine("Default Decrypt succeeded with OAEP-SHA1.");
            }
        }
    }
}

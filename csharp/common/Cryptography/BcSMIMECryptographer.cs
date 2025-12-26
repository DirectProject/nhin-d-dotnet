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
using System.Security.Cryptography.X509Certificates;
using Health.Direct.Common.Mail;
using Health.Direct.Common.Mime;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Security;

namespace Health.Direct.Common.Cryptography
{
    /// <summary>
    /// S/MIME cryptographer using BouncyCastle for CMS operations, enabling RSAES-OAEP on .NET Framework 4.8.
    /// </summary>
    public class BcSMIMECryptographer : ISmimeCryptographer
    {
        public event Action<ISmimeCryptographer, Exception> Error;
        public event Action<ISmimeCryptographer, string> Warning
        {
            add { }
            remove { }
        }

        /// <summary>
        /// For non-critical operations, delegate to the default software cryptographer.
        /// </summary>
        public ISmimeCryptographer DefaultCryptographer { get; set; } = SMIMECryptographer.Default;

        public EncryptionAlgorithm EncryptionAlgorithm { get; set; } = EncryptionAlgorithm.AES128;
        public DigestAlgorithm DigestAlgorithm { get; set; } = DigestAlgorithm.SHA256;
        public bool IncludeMultipartEpilogueInSignature { get; set; } = true;
        public X509IncludeOption IncludeCertChainInSignature { get; set; } = X509IncludeOption.EndCertOnly;

        public MimeEntity Encrypt(MimeEntity entity, X509Certificate2 encryptingCertificate)
        {
            return Encrypt(entity, new X509Certificate2Collection(encryptingCertificate));
        }

        public MimeEntity Encrypt(MimeEntity entity, X509Certificate2Collection encryptingCertificates)
        {
            if (entity == null)
            {
                throw new EncryptionException(EncryptionError.NullEntity);
            }
            if (encryptingCertificates == null || encryptingCertificates.Count == 0)
            {
                throw new EncryptionException(EncryptionError.NoCertificates);
            }

            try
            {
                byte[] messageBytes = DefaultSerializer.Default.SerializeToBytes(entity);
                byte[] encryptedBytes = EncryptBytesWithBc(messageBytes, encryptingCertificates);

                MimeEntity encryptedEntity = new MimeEntity
                {
                    ContentType = SMIMEStandard.EncryptedEnvelopeContentTypeHeaderValue,
                    ContentTransferEncoding = TransferEncoding.Base64.AsString(),
                    Body = new Body(Convert.ToBase64String(encryptedBytes, Base64FormattingOptions.InsertLineBreaks))
                };
                encryptedEntity.ContentDisposition = SMIMEStandard.EncryptedEnvelopeDisposition;
                return encryptedEntity;
            }
            catch (Exception ex)
            {
                this.Error.NotifyEvent(this, ex);
                throw;
            }
        }

        public MimeEntity DecryptEntity(byte[] encryptedBytes, X509Certificate2 decryptingCertificate)
        {
            try
            {
                return DefaultCryptographer.DecryptEntity(encryptedBytes, decryptingCertificate);
            }
            catch (Exception ex)
            {
                this.Error.NotifyEvent(this, ex);
                throw;
            }
        }

        public SignedEntity Sign(Message message, X509Certificate2Collection signingCertificates)
        {
            return DefaultCryptographer.Sign(message, signingCertificates);
        }

        public SignedEntity Sign(MimeEntity entity, X509Certificate2 signingCertificate)
        {
            return DefaultCryptographer.Sign(entity, signingCertificate);
        }

        public SignedEntity Sign(MimeEntity entity, X509Certificate2Collection signingCertificates)
        {
            return DefaultCryptographer.Sign(entity, signingCertificates);
        }

        public System.Security.Cryptography.Pkcs.SignedCms DeserializeDetachedSignature(SignedEntity entity)
        {
            return DefaultCryptographer.DeserializeDetachedSignature(entity);
        }

        public System.Security.Cryptography.Pkcs.SignedCms DeserializeEnvelopedSignature(MimeEntity envelopeEntity)
        {
            return DefaultCryptographer.DeserializeEnvelopedSignature(envelopeEntity);
        }

        public byte[] GetEncryptedBytes(MimeEntity encryptedEntity)
        {
            return DefaultCryptographer.GetEncryptedBytes(encryptedEntity);
        }

        private byte[] EncryptBytesWithBc(byte[] content, X509Certificate2Collection encryptingCertificates)
        {
            CmsEnvelopedDataGenerator gen = new CmsEnvelopedDataGenerator();

            // OAEP parameters for RSA (SHA-256, MGF1 with SHA-256, pSource=PSpecified with empty string)
            AlgorithmIdentifier oaepParams = new AlgorithmIdentifier(
                PkcsObjectIdentifiers.IdRsaesOaep,
                new RsaesOaepParameters(
                    new AlgorithmIdentifier(NistObjectIdentifiers.IdSha256),
                    new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, new AlgorithmIdentifier(NistObjectIdentifiers.IdSha256)),
                    new AlgorithmIdentifier(PkcsObjectIdentifiers.IdPSpecified, new DerOctetString(new byte[0]))
                )
            );

            foreach (X509Certificate2 cert in encryptingCertificates)
            {
                var bcCert = DotNetUtilities.FromX509Certificate(cert);
                // Use Asn1KeyWrapper with OAEP algorithm and public key
                var keyWrapper = new Asn1KeyWrapper(oaepParams, bcCert.GetPublicKey());
                var recipGen = new KeyTransRecipientInfoGenerator(bcCert, keyWrapper);
                gen.AddRecipientInfoGenerator(recipGen);
            }

            string symAlgTag = GetSymmetricEncryptionTag(EncryptionAlgorithm);
            CmsProcessableByteArray data = new CmsProcessableByteArray(content);
            var encrypted = gen.Generate(data, symAlgTag);
            return encrypted.GetEncoded();
        }

        private static string GetSymmetricEncryptionTag(EncryptionAlgorithm alg)
        {
            switch (alg)
            {
                case EncryptionAlgorithm.AES128:
                    return CmsEnvelopedGenerator.Aes128Cbc;
                case EncryptionAlgorithm.AES192:
                    return CmsEnvelopedGenerator.Aes192Cbc;
                case EncryptionAlgorithm.AES256:
                    return CmsEnvelopedGenerator.Aes256Cbc;
                default:
                    return CmsEnvelopedGenerator.Aes128Cbc;
            }
        }
    }
}

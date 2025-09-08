/* 
 Copyright (c) 2016, Direct Project
 All rights reserved.

 Authors:
    Joe Shook      Joseph.Shook@Surescripts.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Utilities;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;
using Health.Direct.Common.Cryptography;
using Health.Direct.Common.Mail;
using Health.Direct.Common.Mime;
using EncryptionException = Health.Direct.Common.Cryptography.EncryptionException;
using IssuerAndSerialNumber = Org.BouncyCastle.Asn1.Cms.IssuerAndSerialNumber;
using RecipientInfo = Org.BouncyCastle.Asn1.Cms.RecipientInfo;
using KeyTransRecipientInfo = Org.BouncyCastle.Asn1.Cms.KeyTransRecipientInfo;
using SignedData = Org.BouncyCastle.Asn1.Cms.SignedData;
using Time = Org.BouncyCastle.Asn1.Cms.Time;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Health.Direct.Hsm
{
    public class HsmCryptographer : SMIMECryptographerBase, ISmimeCryptographer, IDisposable
    {
        private IPkcs11Library m_pkcs11;
        private ISession m_sessionApplication;
        private ISlot m_slot;
        private static readonly Pkcs11InteropFactories _factories = new Pkcs11InteropFactories();

        private TokenSettings m_tokenSettings;
        private bool m_loggedIn;
        private bool m_disposed;

        public event Action<ISmimeCryptographer, Exception> Error;
        public event Action<ISmimeCryptographer, string> Warning;

        public HsmCryptographer() { }

        public TokenSettings TokenSettings
        {
            get => m_tokenSettings;
            set => m_tokenSettings = value;
        }

        public void Init(TokenSettings settings)
        {
            lock (settings)
            {
                try
                {
                    m_tokenSettings = settings;
                    EncryptionAlgorithm = settings.DefaultEncryption;
                    DigestAlgorithm = settings.DefaultDigest;
                    IncludeMultipartEpilogueInSignature = true;
                    IncludeCertChainInSignature = X509IncludeOption.EndCertOnly;

                    InitializePkcs11(settings);
                    EnsureLoggedInSession(settings);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, ex);
                }
            }
        }

        private void InitializePkcs11(TokenSettings settings)
        {
            m_pkcs11 = _factories.Pkcs11LibraryFactory.LoadPkcs11Library(
                _factories,
                settings.Pkcs11LibraryPath,
                settings.UseOsLocking ? AppType.MultiThreaded : AppType.SingleThreaded);

            m_slot = Pkcs11Util.FindSlot(m_pkcs11, settings);

            if (m_slot == null)
                throw new HsmInitException($"Did not find an available slot with TokenLable:{settings.TokenLabel}");
        }

        private void EnsureLoggedInSession(TokenSettings settings)
        {
            m_sessionApplication = m_slot.OpenSession(SessionType.ReadWrite);

            var sessionInfo = m_sessionApplication.GetSessionInfo();
            if (sessionInfo.State != CKS.CKS_RO_USER_FUNCTIONS &&
                sessionInfo.State != CKS.CKS_RW_USER_FUNCTIONS)
            {
                m_sessionApplication.Login(CKU.CKU_USER, settings.NormalUserPin);
            }
            m_loggedIn = true;
        }

        public ISmimeCryptographer DefaultCryptographer { get; set; }

        public MimeEntity Encrypt(MimeEntity entity, X509Certificate2 encryptingCertificate) =>
            throw new NotImplementedException();

        public MimeEntity Encrypt(MimeEntity entity, X509Certificate2Collection encryptingCertificates) =>
            throw new NotImplementedException();

        public MimeEntity DecryptEntity(byte[] encryptedBytes, X509Certificate2 decryptingCertificate)
        {
            try
            {
                if (decryptingCertificate == null)
                    throw new EncryptionException(EncryptionError.NoCertificates);

                var envelopedData = new CmsEnvelopedData(encryptedBytes);
                var envData = EnvelopedData.GetInstance(envelopedData.ContentInfo.Content);

                using (var session = GetSession())
                {
                    if (session == null)
                        return null;

                    foreach (Asn1Sequence asn1Set in envData.RecipientInfos)
                    {
                        var recip = RecipientInfo.GetInstance(asn1Set);
                        var keyTransRecipientInfo = KeyTransRecipientInfo.GetInstance(recip.Info);
                        var sessionKey = Pkcs11Util.Decrypt(session, keyTransRecipientInfo, decryptingCertificate);

#if DEBUG
                        Console.WriteLine(Asn1Dump.DumpAsString(envData));
#endif
                        if (sessionKey == null)
                            continue;

                        var recipientId = new RecipientID();
                        var issuerAndSerialNumber = (IssuerAndSerialNumber)keyTransRecipientInfo.RecipientIdentifier.ID;
                        recipientId.Issuer = issuerAndSerialNumber.Name;
                        recipientId.SerialNumber = issuerAndSerialNumber.SerialNumber.Value;

                        var recipients = envelopedData.GetRecipientInfos().GetRecipients(recipientId);
                        
                        var encInfo = envData.EncryptedContentInfo;
                        var encAlg = encInfo.ContentEncryptionAlgorithm;
                        var readable = new CmsProcessableByteArray(encInfo.EncryptedContent.GetOctets());
                        var keyParameter = ParameterUtilities.CreateKeyParameter(encAlg.Algorithm.Id, sessionKey);

                        foreach (RecipientInformation _ in recipients)
                        {
                            var cmsReadable = GetReadable((KeyParameter)keyParameter, encAlg, readable);
                            var cmsTypedStream = new CmsTypedStream(cmsReadable.GetInputStream());
                            var contentBytes = StreamToByteArray(cmsTypedStream.ContentStream);
                            return MimeSerializer.Default.Deserialize<MimeEntity>(contentBytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
            return null;
        }

        public SignedEntity Sign(Message message, X509Certificate2Collection signingCertificates) =>
            Sign(message.ExtractEntityForSignature(IncludeMultipartEpilogueInSignature), signingCertificates);

        public CmsReadable GetReadable(KeyParameter sKey, Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier algorithm, CmsProcessableByteArray readable)
        {
            IBufferedCipher cipher;
            try
            {
                cipher = CipherUtilities.GetCipher(algorithm.Algorithm);
                var asn1Enc = algorithm.Parameters;
                var asn1Params = asn1Enc == null ? null : asn1Enc.ToAsn1Object();
                ICipherParameters cipherParameters = sKey;

                if (asn1Params != null && !(asn1Params is Asn1Null))
                {
                    cipherParameters = ParameterUtilities.GetCipherParameters(algorithm.Algorithm, cipherParameters, asn1Params);
                }
                else
                {
                    string alg = algorithm.Algorithm.Id;
                    if (alg.Equals(CmsEnvelopedGenerator.DesEde3Cbc) ||
                        alg.Equals(CmsEnvelopedGenerator.IdeaCbc) ||
                        alg.Equals(CmsEnvelopedGenerator.Cast5Cbc))
                    {
                        cipherParameters = new ParametersWithIV(cipherParameters, new byte[8]);
                    }
                }
                cipher.Init(false, cipherParameters);
            }
            catch (SecurityUtilityException e) { throw new CmsException("couldn't create cipher.", e); }
            catch (InvalidKeyException e) { throw new CmsException("key invalid in message.", e); }
            catch (IOException e) { throw new CmsException("error decoding algorithm parameters.", e); }

            try
            {
                return new CmsProcessableInputStream(new CipherStream(readable.GetInputStream(), cipher, null));
            }
            catch (IOException e)
            {
                throw new CmsException("error reading content.", e);
            }
        }

        public static byte[] StreamToByteArray(Stream inStream) => Streams.ReadAll(inStream);

        public SignedEntity Sign(MimeEntity entity, X509Certificate2 signingCertificate)
        {
            try { return Sign(entity, new X509Certificate2Collection(signingCertificate)); }
            catch (Exception ex) { Error?.Invoke(this, ex); return null; }
        }

        private byte[] Sign(byte[] content, X509Certificate2Collection signingCertificates)
        {
#if DEBUG
            Console.WriteLine(signingCertificates[0].ToString(true));
#endif
            using (var session = GetSession())
            {
                if (session == null)
                    return null;

                var signature = CreateSignature(session, content, signingCertificates);
                var contentInfo = new Org.BouncyCastle.Asn1.Pkcs.ContentInfo(
                    new DerObjectIdentifier(PkcsObjectIdentifiers.SignedData.Id),
                    signature);
                return contentInfo.GetDerEncoded();
            }
        }

        private ISession GetSession()
        {
            // Fast path: we think we are logged in and have a session + slot
            if (m_slot != null && m_loggedIn && m_sessionApplication != null)
            {
                try
                {
                    // Probe the existing application session to ensure token connectivity
                    var state = m_sessionApplication.GetSessionInfo().State;

                    // Option 1: reuse the application session
                    // return m_sessionApplication;

                    // Option 2 (original behavior): open a fresh RW session each use
                    return m_slot.OpenSession(SessionType.ReadWrite);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, ex);

                    // Mark as not logged in; we'll attempt recovery below
                    lock (m_tokenSettings)
                    {
                        try
                        {
                            // Another probe in case the first failure was transient
                            var state = m_sessionApplication.GetSessionInfo().State;
                            if (state != CKS.CKS_RO_USER_FUNCTIONS && state != CKS.CKS_RW_USER_FUNCTIONS)
                                m_loggedIn = false;
                        }
                        catch
                        {
                            m_loggedIn = false;
                        }
                    }
                }
            }

            // Recovery path
            lock (m_tokenSettings)
            {
                try
                {
                    Warning?.Invoke(this, "Attempting to reconnect to token");

                    m_pkcs11?.Dispose();
                    m_slot = null;
                    m_sessionApplication = null;

                    InitializePkcs11(m_tokenSettings);
                    EnsureLoggedInSession(m_tokenSettings);

                    return m_slot.OpenSession(SessionType.ReadWrite);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(this, ex);
                    return null;
                }
            }
        }

        private SignedData CreateSignature(ISession session, byte[] content, X509Certificate2Collection signingCertificates)
        {
            var digestOid = ToDigestAlgorithmOid(DigestAlgorithm).Value;
            var hashGenerator = GetHashGenerator(DigestAlgorithm);
            var dataHash = ComputeDigest(hashGenerator, content);

            var signedAttributesVector = new Asn1EncodableVector();
            signedAttributesVector.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                new DerObjectIdentifier(PkcsObjectIdentifiers.Pkcs9AtContentType.Id),
                new DerSet(new DerObjectIdentifier(PkcsObjectIdentifiers.Data.Id))));
            signedAttributesVector.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                new DerObjectIdentifier(PkcsObjectIdentifiers.Pkcs9AtMessageDigest.Id),
                new DerSet(new DerOctetString(dataHash))));
            signedAttributesVector.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                new DerObjectIdentifier(PkcsObjectIdentifiers.Pkcs9AtSigningTime.Id),
                new DerSet(new Time(new DerUtcTime(DateTime.UtcNow)))));
            var signedAttributes = new DerSet(signedAttributesVector);

            var pkcs1Digest = ComputeDigest(hashGenerator, signedAttributes.GetDerEncoded());
            var pkcs1DigestInfo = CreateDigestInfo(pkcs1Digest, digestOid);

            var digestAlgorithmsVector = new Asn1EncodableVector();
            var certificatesVector = new Asn1EncodableVector();
            var signerInfosVector = new Asn1EncodableVector();

            var encapContentInfo = new Org.BouncyCastle.Asn1.Cms.ContentInfo(
                new DerObjectIdentifier(PkcsObjectIdentifiers.Data.Id),
                null);

            foreach (var signingCertificate in signingCertificates)
            {
                var bcSigningCertificate = CertificateUtilities.ToBouncyCastleObject(signingCertificate.RawData);
                var pubKeyParams = bcSigningCertificate.GetPublicKey();
                if (!(pubKeyParams is RsaKeyParameters rsaPubKeyParams))
                    throw new NotSupportedException("Unsupported keys. Currently RSA only.");

                byte[] pkcs1Signature = signingCertificate.HasPrivateKey
                    ? GeneratePkcs1Signature(signingCertificate, pkcs1DigestInfo)
                    : GeneratePkcs1Signature(session, rsaPubKeyParams, bcSigningCertificate, pkcs1DigestInfo);

                var signerInfo = new Org.BouncyCastle.Asn1.Cms.SignerInfo(
                    new SignerIdentifier(new IssuerAndSerialNumber(bcSigningCertificate.IssuerDN, bcSigningCertificate.SerialNumber)),
                    new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier(digestOid), null),
                    signedAttributes,
                    new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier(PkcsObjectIdentifiers.RsaEncryption.Id), null),
                    new DerOctetString(pkcs1Signature),
                    null);

                digestAlgorithmsVector.Add(new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier(digestOid), null));
                certificatesVector.Add(X509CertificateStructure.GetInstance(Asn1Object.FromByteArray(bcSigningCertificate.GetEncoded())));
                signerInfosVector.Add(signerInfo.ToAsn1Object());
            }

            return new SignedData(
                new DerSet(digestAlgorithmsVector),
                encapContentInfo,
                new BerSet(certificatesVector),
                null,
                new DerSet(signerInfosVector));
        }

        private static byte[] GeneratePkcs1Signature(ISession session, RsaKeyParameters rsaPubKeyParams,
            X509Certificate bcSigningCertificate, byte[] pkcs1DigestInfo)
        {
            var attrFactory = _factories.ObjectAttributeFactory;
            var attributes = new List<IObjectAttribute>();
            try
            {
                attributes.Add(attrFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY));
                attributes.Add(attrFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_RSA));
                attributes.Add(attrFactory.Create(CKA.CKA_MODULUS, rsaPubKeyParams.Modulus.ToByteArrayUnsigned()));
                attributes.Add(attrFactory.Create(CKA.CKA_PUBLIC_EXPONENT, rsaPubKeyParams.Exponent.ToByteArrayUnsigned()));

                var privateKeyHandles = session.FindAllObjects(attributes);

                if (privateKeyHandles.Count != 1)
                    throw new HsmObjectNotFoundException(
                        $"Private key correlation failed for signing cert\r\n{bcSigningCertificate}\r\nCKA_MODULUS: {rsaPubKeyParams.Modulus}\r\nCKA_PUBLIC_EXPONENT {rsaPubKeyParams.Exponent}");

                using (var mechanism = _factories.MechanismFactory.Create(CKM.CKM_RSA_PKCS))
                {
                    return session.Sign(mechanism, privateKeyHandles.Single(), pkcs1DigestInfo);
                }
            }
            finally
            {
                foreach (var a in attributes)
                    a.Dispose();
            }
        }

        private static byte[] GeneratePkcs1Signature(X509Certificate2 signingCertificate, byte[] pkcs1DigestInfo)
        {
            var key = DotNetUtilities.GetKeyPair(signingCertificate.PrivateKey);
            ISigner signer = SignerUtilities.GetSigner("RSA");
            signer.Init(true, key.Private);
            signer.BlockUpdate(pkcs1DigestInfo, 0, pkcs1DigestInfo.Length);
            return signer.GenerateSignature();
        }

        private byte[] ComputeDigest(IDigest hashGenerator, byte[] content)
        {
            if (hashGenerator == null) throw new ArgumentNullException(nameof(hashGenerator));
            if (content == null) throw new ArgumentNullException(nameof(content));

            var hash = new byte[hashGenerator.GetDigestSize()];
            hashGenerator.Reset();
            hashGenerator.BlockUpdate(content, 0, content.Length);
            hashGenerator.DoFinal(hash, 0);
            return hash;
        }

        private static IDigest GetHashGenerator(DigestAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case DigestAlgorithm.SHA1: return new Sha1Digest();
                case DigestAlgorithm.SHA256: return new Sha256Digest();
                case DigestAlgorithm.SHA384: return new Sha384Digest();
                case DigestAlgorithm.SHA512: return new Sha512Digest();
                default: throw new NotSupportedException("Unsupported hash algorithm");
            }
        }

        private static byte[] CreateDigestInfo(byte[] digest, string digestOid)
        {
            var derObjectIdentifier = new DerObjectIdentifier(digestOid);
            var algorithmIdentifier = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(derObjectIdentifier, null);
            var digestInfo = new DigestInfo(algorithmIdentifier, digest);
            return digestInfo.GetDerEncoded();
        }

        public SignedEntity Sign(MimeEntity entity, X509Certificate2Collection signingCertificates)
        {
            if (entity == null)
                throw new Common.Cryptography.SignatureException(SignatureError.NullEntity);

            var entityBytes = MimeSerializer.Default.SerializeToBytes(entity);
            var signature = CreateSignatureEntity(entityBytes, signingCertificates);
            return signature == null ? null : new SignedEntity(DigestAlgorithm, entity, signature);
        }

        private MimeEntity CreateSignatureEntity(byte[] content, X509Certificate2Collection signingCertificates)
        {
            var signatureBytes = Sign(content, signingCertificates);
            if (signatureBytes == null)
                return null;

            var signature = new MimeEntity
            {
                ContentType = SMIMEStandard.SignatureContentTypeHeaderValue,
                ContentTransferEncoding = TransferEncoding.Base64.AsString(),
                Body = new Body(Convert.ToBase64String(signatureBytes))
            };
            signature.ContentDisposition = SMIMEStandard.SignatureDisposition;
            return signature;
        }

        public SignedCms DeserializeDetachedSignature(SignedEntity entity) =>
            throw new NotImplementedException();

        public SignedCms DeserializeEnvelopedSignature(MimeEntity envelopeEntity) =>
            throw new NotImplementedException();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                    m_pkcs11?.Dispose();
                m_disposed = true;
            }
        }

        ~HsmCryptographer() => Dispose(false);
    }

    public class HsmObjectNotFoundException : Exception
    {
        public HsmObjectNotFoundException(string message) : base(message) { }
    }

    public class HsmInitException : Exception
    {
        public HsmInitException(string message) : base(message) { }
    }
}

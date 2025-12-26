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
using System.Security.Cryptography.X509Certificates;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace Health.Direct.Hsm
{
    /// <summary>
    /// Pkcs#11 Utilities
    /// </summary>
    public class Pkcs11Util
    {
        private static readonly Pkcs11InteropFactories Factories = new Pkcs11InteropFactories();

        /// <summary>
        /// Finds slot containing the token that matches criteria specified in <see cref="TokenSettings"/> class
        /// </summary>
        /// <param name='pkcs11'>Initialized PKCS11 wrapper</param>
        /// <param name="settings"></param>
        /// <returns>Slot containing the token that matches criteria in <see cref="TokenSettings"/></returns>
        public static ISlot FindSlot(IPkcs11Library pkcs11, TokenSettings settings)
        {
            // Get list of available slots with token present
            var slots = pkcs11.GetSlotList(SlotsType.WithTokenPresent);

            // No criteria, not go.
            if (settings.TokenLabel == null)
                return null;

            foreach (var slot in slots)
            {
                ITokenInfo tokenInfo = null;

                try
                {
                    tokenInfo = slot.GetTokenInfo();
                }
                catch (Pkcs11Exception ex)
                {
                    if (ex.RV != CKR.CKR_TOKEN_NOT_RECOGNIZED &&
                        ex.RV != CKR.CKR_TOKEN_NOT_PRESENT)
                        throw;
                }

                if (tokenInfo == null)
                    continue;

                if (!String.IsNullOrEmpty(settings.TokenLabel))
                {
                    if (0 != String.Compare(
                        settings.TokenLabel,
                        tokenInfo.Label,
                        StringComparison.Ordinal))
                        continue;
                }

                return slot;
            }

            return null;
        }

        public static byte[] Decrypt(ISession session, KeyTransRecipientInfo keyTransRecipientInfo, X509Certificate2 cert)
        {
            var x509CertificateParser = new X509CertificateParser();
            var x509Certificate = x509CertificateParser.ReadCertificate(cert.RawData);

            // Get public key from certificate
            var pubKeyParams = x509Certificate.GetPublicKey(); //AsymmetricKeyParameter
            if (!(pubKeyParams is RsaKeyParameters))
                throw new NotSupportedException("Unsupported keys.  Currently supporting RSA keys only.");

            var rsaPubKeyParams = (RsaKeyParameters)pubKeyParams;

            var attrFactory = Factories.ObjectAttributeFactory;
            var privKeySearchTemplate = new List<IObjectAttribute>();
            try
            {
                privKeySearchTemplate.Add(attrFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY));
                privKeySearchTemplate.Add(attrFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_RSA));
                privKeySearchTemplate.Add(attrFactory.Create(CKA.CKA_MODULUS, rsaPubKeyParams.Modulus.ToByteArrayUnsigned()));
                privKeySearchTemplate.Add(attrFactory.Create(CKA.CKA_PUBLIC_EXPONENT, rsaPubKeyParams.Exponent.ToByteArrayUnsigned()));

                var hsmObjects = session.FindAllObjects(privKeySearchTemplate);
                var encryptedKey = keyTransRecipientInfo.EncryptedKey.GetOctets();
                var oid = keyTransRecipientInfo.KeyEncryptionAlgorithm.Algorithm.Id;

                using (var mechanism = SelectMechanism(oid, keyTransRecipientInfo))
                {
                    foreach (var handle in hsmObjects)
                    {
                        try
                        {
                            return session.Decrypt(mechanism, handle, encryptedKey);
                        }
                        catch
                        {
                            // try next key
                        }
                    }
                }
            }
            finally
            {
                foreach (var a in privKeySearchTemplate)
                    a.Dispose();
            }

            return null;
        }

        // Enhanced: detects OAEP params hash if present, otherwise defaults to SHA-1
        private static IMechanism SelectMechanism(string oid, KeyTransRecipientInfo keyTransRecipientInfo = null)
        {
            var mechFactory = Factories.MechanismFactory;
            var paramsFactory = Factories.MechanismParamsFactory;

            if (oid == PkcsObjectIdentifiers.IdRsaesOaep.Id)
            {
                // Default values (CMS often omits explicit OAEP params -> implies SHA-1)
                CKM hashAlg = CKM.CKM_SHA_1;
                CKG mgf = CKG.CKG_MGF1_SHA1;

                // OPTIONAL: Parse RSAES-OAEP-params if present to upgrade (SHA-256 etc.)
                // BouncyCastle provides KeyEncryptionAlgorithm.Parameters (AlgorithmIdentifier sequence)
                // Only attempt if parameters are supplied.
                var algIdParams = keyTransRecipientInfo?.KeyEncryptionAlgorithm?.Parameters;
                // You can enhance by inspecting algIdParams and mapping OIDs to CKM/CKG if needed.

                var oaepParams = paramsFactory.CreateCkRsaPkcsOaepParams(
                    (ulong)hashAlg,              // CKM enum -> ulong
                    (ulong)mgf,                  // CKG enum -> ulong
                    (ulong)CKZ.CKZ_DATA_SPECIFIED,
                    null);

                return mechFactory.Create(CKM.CKM_RSA_PKCS_OAEP, oaepParams);
            }

            if (oid == PkcsObjectIdentifiers.RsaEncryption.Id)
            {
                return mechFactory.Create(CKM.CKM_RSA_PKCS);
            }

            throw new NotSupportedException($"No supported HSM mechanisms for RSA key transport OID {oid}");
        }
    }
}

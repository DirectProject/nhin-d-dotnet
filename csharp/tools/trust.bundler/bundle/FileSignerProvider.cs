/* 
 Copyright (c) 2013-2025, Direct Project
 All rights reserved.

 Authors:
    Joe Shook     jshook@kryptiq.com
    Joseph Shook      Joseph.Shook@Surescripts.com

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/

using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Health.Direct.Trust
{
    /// <summary>
    /// File system implementation of ISignProvider 
    /// </summary>
    public class FileSignerProvider : ISignProvider
    {
        private readonly string _signature;
        private readonly SecureString _key;

        /// <summary>
        /// Create a new FileSignerProvider
        /// </summary>
        /// <param name="signature">Signature/s file or folder location</param>
        /// <param name="key">Optional secure key to signature.</param>
        public FileSignerProvider(string signature, SecureString key = null)
        {
            _signature = signature;
            _key = key;
        }

        public string Signature
        {
            get { return _signature; }
        }

        public SecureString Key
        {
            get { return _key; }
        }

        public byte[] Sign(byte[] cmsData)
        {
            // Load signing cert (PFX)
            byte[] signBytes = File.ReadAllBytes(GetFile());
            var signCert = new X509Certificate2(
                signBytes,
                Key,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            // Build PKCS#7 SignedCms using Windows crypto (CSP/CNG) to avoid exporting private key
            var content = new ContentInfo(cmsData);
            var signedCms = new SignedCms(content, false);

            // SHA-1 signer (legacy)
            var sha1Signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signCert)
            {
                IncludeOption = X509IncludeOption.EndCertOnly
            };
            sha1Signer.DigestAlgorithm = new Oid("1.3.14.3.2.26"); // SHA-1
            signedCms.ComputeSignature(sha1Signer, false);

            // SHA-256 signer
            var sha256Signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signCert)
            {
                IncludeOption = X509IncludeOption.EndCertOnly
            };
            sha256Signer.DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"); // SHA-256
            signedCms.ComputeSignature(sha256Signer, false);

            return signedCms.Encode();
        }

        private string GetFile()
        {
            if (File.Exists(Signature))
            {
                return Signature;
            }
            return Directory.GetFiles(Signature).First();
        }
    }
}
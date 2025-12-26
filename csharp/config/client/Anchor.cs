/* 
 Copyright (c) 2010-2025, Direct Project
 All rights reserved.

 Authors:
    Joe Shook       Joseph.Shook@Surescripts.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/

using System;
using System.Security.Cryptography.X509Certificates;
using Health.Direct.Common;
using Health.Direct.Common.Certificates;
using Health.Direct.Common.Extensions;

namespace Health.Direct.Config.Client.CertificateService
{
    public partial class Anchor
    {
        public bool HasData => (this.Data != null && this.Data.Length > 0);

        public Anchor(string owner, byte[] sourceFileBytes, string password)
            : this(owner, Certificate.Import(sourceFileBytes, password))
        {
        }


        public Anchor(string owner, X509Certificate2 certificate)
            : this(owner, certificate, true, true)
        {
        }

        public Anchor(string owner, X509Certificate2 certificate, bool forIncoming, bool forOutgoing)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            this.Owner = owner;
            this.Thumbprint = certificate.Thumbprint;
            this.Data = certificate.RawData;
            this.CreateDate = DateTimeHelper.Now;
            this.ValidStartDate = certificate.NotBefore;
            this.ValidEndDate = certificate.NotAfter;
            this.ForIncoming = forIncoming;
            this.ForOutgoing = forOutgoing;
        }

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
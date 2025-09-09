/* 
 Copyright (c) 2010, Direct Project
 All rights reserved.

 Authors:
    Chris Lomonico chris.lomonico@surescripts.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Health.Direct.Common.DnsResolver;
using Xunit;

namespace Health.Direct.Common.Tests.Caching
{
    public class DnsResponseToBinExample
    {
        private readonly DnsClient m_client;
        private const string DNSRECORDSEPATH = @"..\..\..\common.metadata\DnsRecords";
        private const string DnsResponsePath = @"..\..\..\common.metadata\dnsresponses";

        const string PublicDns = "8.8.8.8";         // Google

        public class CertData
        {
            private string m_key = string.Empty;
            private string m_friendlyname = string.Empty;
            private string m_distinguishedName = string.Empty;
            private string m_password = string.Empty;

            public string Key
            {
                get { return m_key; }
                set { m_key = value; }
            }

            public string Friendlyname
            {
                get { return m_friendlyname; }
                set { m_friendlyname = value; }
            }

            public string DistinguishedName
            {
                get { return m_distinguishedName; }
                set { m_distinguishedName = value; }
            }

            public string Password
            {
                get { return m_password; }
                set { m_password = value; }
            }

            public CertData(string key
                , string friendlyName
                , string distinguishedName
                , string password)
            {
                m_key = key;
                m_friendlyname = friendlyName;
                m_distinguishedName = distinguishedName;
                m_password = password;
            }
        }

        public static IEnumerable<object[]> CertDataVals
        {
            get
            {
                for (int i = 1; i <= 10; i++)
                {
                    for (int t = 1; t <= 3; t++)
                    {
                        yield return new[] { new CertData(string.Format("domain{0}.test.com", i)
                         , string.Format("domain{0}.test.com", i)
                         , string.Format("CN=domain{0}.test.com", i)
                         , "")};
                    }
                }

            }
        }

        public DnsResponseToBinExample()
        {

            m_client = new DnsClient(PublicDns) { Timeout = TimeSpan.FromSeconds(10) };
        }

        //---these "tests" are purely examples of how to create mock responses, they are skipped out as they alter the file system 
        //---(see the paths above).  Alter and use as needed
        [Theory(Skip = "Alters file system")]
        [InlineData("www.microsoft.com")]
        [InlineData("www.yahoo.com")]
        [InlineData("www.google.com")]
        [InlineData("www.apple.com")]
        [InlineData("www.bing.com")]
        [InlineData("www.nhindirect.org")]
        [InlineData("www.epic.com")]
        [InlineData("www.cerner.com")]
        [InlineData("www.ibm.com")]
        public void CreateAResponseDumps(string domain)
        {
            DnsBuffer buff = new DnsBuffer(DnsStandard.MaxUdpMessageLength * 2);
            m_client.Resolve(DnsRequest.CreateA(domain)).Serialize(buff);
            byte[] bytes = buff.CreateReader().ReadBytes();
            string path = Path.Combine(DnsResponsePath, string.Format("aname.{0}.bin", domain)).Replace("www.", "");
            Console.WriteLine("Creating {0}", path);
            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(bytes
                        , 0
                        , bytes.Length);
                s.Close();
            }

        }

        [Theory(Skip = "Alters file system")]
        [InlineData("www.microsoft.com")]
        [InlineData("www.yahoo.com")]
        [InlineData("www.google.com")]
        [InlineData("www.apple.com")]
        [InlineData("www.bing.com")]
        [InlineData("www.nhindirect.org")]
        [InlineData("www.epic.com")]
        [InlineData("www.cerner.com")]
        [InlineData("www.ibm.com")]
        public void CreateSOAResponseDumps(string domain)
        {
            DnsBuffer buff = new DnsBuffer(DnsStandard.MaxUdpMessageLength * 2);
            m_client.Resolve(DnsRequest.CreateSOA(domain)).Serialize(buff);
            byte[] bytes = buff.CreateReader().ReadBytes();
            string path = Path.Combine(DnsResponsePath, string.Format("soa.{0}.bin", domain)).Replace("www.", "");
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(bytes
                        , 0
                        , bytes.Length);
                s.Close();
            }
        }

        [Theory(Skip = "Alters file system")]
        [InlineData("www.microsoft.com")]
        [InlineData("www.yahoo.com")]
        [InlineData("www.google.com")]
        [InlineData("www.apple.com")]
        [InlineData("www.bing.com")]
        [InlineData("www.nhindirect.org")]
        [InlineData("www.epic.com")]
        [InlineData("www.cerner.com")]
        [InlineData("www.ibm.com")]
        public void CreateMXResponseDumps(string domain)
        {
            DnsBuffer buff = new DnsBuffer(DnsStandard.MaxUdpMessageLength * 2);
            m_client.Resolve(DnsRequest.CreateMX(domain)).Serialize(buff);
            byte[] bytes = buff.CreateReader().ReadBytes();
            string path = Path.Combine(DnsResponsePath, string.Format("mx.{0}.bin", domain)).Replace("www.", "");
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(bytes
                        , 0
                        , bytes.Length);
                s.Close();
            }
        }

        [Theory(Skip = "Alters file system")]
        [InlineData("www.microsoft.com")]
        [InlineData("www.yahoo.com")]
        [InlineData("www.google.com")]
        [InlineData("www.apple.com")]
        [InlineData("www.bing.com")]
        [InlineData("www.nhindirect.org")]
        [InlineData("www.epic.com")]
        [InlineData("www.cerner.com")]
        [InlineData("www.ibm.com")]
        public void CreateCertResponseDumps(string domain)
        {
            DnsBuffer buff = new DnsBuffer(DnsStandard.MaxUdpMessageLength * 2);
            m_client.Resolve(DnsRequest.CreateCERT(domain)).Serialize(buff);
            byte[] bytes = buff.CreateReader().ReadBytes();
            string path = Path.Combine(DnsResponsePath, string.Format("cert.{0}.bin", domain)).Replace("www.", "");
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(bytes
                        , 0
                        , bytes.Length);
                s.Close();
            }
        }

        [Theory(Skip = "Alters file system")]
        [InlineData("microsoft.com")]
        [InlineData("yahoo.com")]
        [InlineData("google.com")]
        [InlineData("apple.com")]
        [InlineData("bing.com")]
        [InlineData("nhindirect.org")]
        [InlineData("epic.com")]
        [InlineData("cerner.com")]
        [InlineData("ibm.com")]
        public void CreateDnsResourceRecords(string domain)
        {
            DnsBuffer buff = new DnsBuffer();
            byte[] bytes;
            AddressRecord arec = new AddressRecord(domain
                , "127.0.0.1")
            { TTL = 1000 };
            arec.Serialize(buff);

            string path = Path.Combine(DNSRECORDSEPATH, string.Format("aname.{0}.bin", domain));
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(buff.Buffer
                        , 0
                        , buff.Buffer.Length);
                s.Close();
            }


            //----------------------------------------------------------------------------------------------------
            //---read the stream from the bytes
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("checking [{0}]", path);
                bytes = new BinaryReader(fs).ReadBytes((int)new FileInfo(path).Length);
                DnsBufferReader rdr = new DnsBufferReader(bytes, 0, bytes.Length);
                arec = (AddressRecord)DnsResourceRecord.Deserialize(ref rdr);
            }
            Console.WriteLine(arec.IPAddress);
            Console.WriteLine(arec.TTL);
            Console.WriteLine(arec.Name);
            //----------------------------------------------------------------------------------------------------------------
            SOARecord soa = new SOARecord(domain
                , domain + ".dom"
                , "somebody"
                , 1
                , 2
                , 3
                , 4
                , 5)
            { TTL = 2000 };
            buff = new DnsBuffer();
            soa.Serialize(buff);

            path = Path.Combine(DNSRECORDSEPATH, string.Format("soa.{0}.bin", domain));
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(buff.Buffer
                        , 0
                        , buff.Buffer.Length);
                s.Close();
            }

            //----------------------------------------------------------------------------------------------------
            //---read the stream from the bytes
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("checking [{0}]", path);
                bytes = new BinaryReader(fs).ReadBytes((int)new FileInfo(path).Length);
                DnsBufferReader rdr = new DnsBufferReader(bytes, 0, bytes.Length);
                soa = (SOARecord)DnsResourceRecord.Deserialize(ref rdr);
            }
            Console.WriteLine(soa.ResponsibleName);
            Console.WriteLine(soa.SerialNumber);
            Console.WriteLine(soa.Retry);
            Console.WriteLine(soa.Refresh);
            Console.WriteLine(soa.Expire);
            Console.WriteLine(soa.Minimum);
            Console.WriteLine(soa.TTL);
            Console.WriteLine(soa.Name);
            //----------------------------------------------------------------------------------------------------------------
            MXRecord mx = new MXRecord(domain
                , string.Format("mx.{0}", domain)
                , 1)
            { TTL = 2000 };

            buff = new DnsBuffer();
            mx.Serialize(buff);

            path = Path.Combine(DNSRECORDSEPATH, string.Format("mx.{0}.bin", domain));
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(buff.Buffer
                        , 0
                        , buff.Buffer.Length);
                s.Close();
            }

            //----------------------------------------------------------------------------------------------------
            //---read the stream from the bytes
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("checking [{0}]", path);
                bytes = new BinaryReader(fs).ReadBytes((int)new FileInfo(path).Length);
                DnsBufferReader rdr = new DnsBufferReader(bytes, 0, bytes.Length);
                mx = (MXRecord)DnsResourceRecord.Deserialize(ref rdr);
            }
            Console.WriteLine(mx.Exchange);
            Console.WriteLine(mx.Name);
            Console.WriteLine(mx.Preference);

            //----------------------------------------------------------------------------------------------------------------
            //---create the cert on the fly
            CertRecord cert = new CertRecord(new DnsX509Cert(CreateNamedKeyCertificate(new CertData(domain
                , domain
                , string.Format("CN={0}", domain)
                , ""))))
            { TTL = 2000 };

            buff = new DnsBuffer();
            cert.Serialize(buff);

            path = Path.Combine(DNSRECORDSEPATH, string.Format("cert.{0}.bin", domain));
            Console.WriteLine("Creating {0}", path);

            using (FileStream s = new FileStream(path, FileMode.OpenOrCreate))
            {
                s.Write(buff.Buffer
                        , 0
                        , buff.Buffer.Length);
                s.Close();
            }

            //----------------------------------------------------------------------------------------------------
            //---read the stream from the bytes
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("checking [{0}]", path);
                bytes = new BinaryReader(fs).ReadBytes((int)new FileInfo(path).Length);
                DnsBufferReader rdr = new DnsBufferReader(bytes, 0, bytes.Length);
                cert = (CertRecord)DnsResourceRecord.Deserialize(ref rdr);
            }
            Console.WriteLine(cert.Name);
            Console.WriteLine(cert.Cert.Certificate.NotBefore);
            Console.WriteLine(cert.Cert.Certificate.NotAfter);
        }

        public X509Certificate2 CreateNamedKeyCertificate(CertData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            X509Certificate2 cert = null;
            try
            {
                // Prepare date range (same fixed dates as original code)
                var notBefore = DateTime.Parse(
                    "01/01/2010",
                    System.Globalization.DateTimeFormatInfo.InvariantInfo,
                    System.Globalization.DateTimeStyles.AssumeLocal);

                var notAfter = DateTime.Parse(
                    "01/01/2020",
                    System.Globalization.DateTimeFormatInfo.InvariantInfo,
                    System.Globalization.DateTimeStyles.AssumeLocal);

                // Create a named CNG RSA key (persisted) with liberal export policies like original
                var keyCreationParameters = new CngKeyCreationParameters
                {
                    ExportPolicy =
                        CngExportPolicies.AllowExport |
                        CngExportPolicies.AllowPlaintextExport |
                        CngExportPolicies.AllowPlaintextArchiving |
                        CngExportPolicies.AllowArchiving,
                    KeyUsage = CngKeyUsages.AllUsages,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider
                };

                using (var cngKey = CngKey.Create(CngAlgorithm.Rsa, data.Key, keyCreationParameters))
                using (var rsa = new RSACng(cngKey))
                {
                    // Build certificate request
                    var dn = new X500DistinguishedName(data.DistinguishedName);
                    // Choose SHA256 (old helper chose defaults; adjust if you need a different digest)
                    var request = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    // (Optional) Add minimal extensions – keep it simple to mirror legacy behavior
                    // request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                    // request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

                    cert = request.CreateSelfSigned(notBefore, notAfter);

                    cert.FriendlyName = data.Friendlyname;

                    // Assertions equivalent to original intent
                    Assert.True(cert.HasPrivateKey);
                    using (var pk = cert.GetRSAPrivateKey())
                    {
                        Assert.NotNull(pk);
                        // Ensure backing implementation is CNG (RSACng) to approximate HasCngKey()
                        Assert.IsType<RSACng>(pk);
                    }
                }

                return cert;
            }
            finally
            {
                // Clean up the persisted named key like original logic did
                try
                {
                    if (CngKey.Exists(data.Key))
                    {
                        using (var k = CngKey.Open(data.Key))
                        {
                            k.Delete();
                        }
                    }
                }
                catch
                {
                    // Swallow any cleanup errors to keep legacy behavior tolerant
                }
            }
        }
    }
}
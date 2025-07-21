using System;
using Health.Direct.Common.DnsResolver;
using Health.Direct.Common.Extensions;

namespace Health.Direct.Config.Client.RecordRetrieval
{
    public partial class DnsRecord
    {
        public DnsResourceRecord Deserialize()
        {
            if (this.RecordData.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Empty record data found.");
            }

            DnsBufferReader bufferReader = new DnsBufferReader(this.RecordData, 0, this.RecordData.Length);
            return DnsResourceRecord.Deserialize(ref bufferReader);
        }

        /// <summary>
        /// Deserialize the raw ResourceRecord embedded in this DnsRecord
        /// </summary>
        /// <typeparam name="T">Of type DnsResourceRecord</typeparam>
        /// <returns>DnsResourceRecord</returns>
        public T Deserialize<T>()
            where T : DnsResourceRecord
        {
            T record = this.Deserialize() as T;
            if (record == null)
            {
                throw new ArgumentException(
                    $"Returned record type does not match expected type, found {this.RecordType}");
            }

            return record;
        }

        public DnsStandardRecordType RecordType
        {
            get
            {
                return (DnsStandardRecordType)this.TypeID;
            }
        }
    }
}
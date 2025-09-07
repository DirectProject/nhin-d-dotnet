using System;
using System.Net.Mail;
using Health.Direct.Common.Mail;

namespace Health.Direct.Config.Client.DomainManager
{
    public partial class Address
    {
        public bool Match(MailAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
            }

            return this.Match(address.Address);
        }

        public bool Match(string emailAddress)
        {
            return MailStandard.Equals(EmailAddress, emailAddress);
        }

    }
}
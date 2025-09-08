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

using Health.Direct.Common.Mail;
using Health.Direct.Common.Mail.DSN;
using Health.Direct.Config.Store;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Transactions;

namespace Health.Direct.MdnMonitor
{
    /// <summary>
    /// Find expired processed MDNs, mark expired and send failed DSNs
    /// </summary>
    public class MdnProcessedTimeout : Timeout, IJob //Will limit to one job at a time.
    {
        /// <summary>
        /// Entry point called when trigger fires.
        /// </summary>
        /// <param name="context"></param>
        public Task Execute(IJobExecutionContext context)
        {
            var settings = Load(context);
            
            try
            {
                foreach (var mdn in ExpiredMdns(settings))
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        var message = CreateNotificationMessage(mdn, settings);
                        string filePath = Path.Combine(settings.PickupFolder, UniqueFileName());
                        MDNManager.TimeOut(mdn);
                        message.Save(filePath);
                        scope.Complete();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error in job!");
                Logger.Error(e.Message);
                var je = new JobExecutionException(e);
                throw je;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieve expired records.
        ///     Records without processed notification and older than the <c>ExpiredMinutes</c>
        ///     Load a limited amount of record set by <c>BulkCount</c>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected override IList<Mdn> ExpiredMdns(TimeoutSettings settings)
        {
            IList<Mdn> mdns;

            mdns = MDNManager.GetExpiredProcessed(settings.ExpiredMinutes, settings.BulkCount).ToList();
            if (mdns.Count() > 0)
            {
                Logger.Debug("Processing {0} expired processed mdns", mdns.Count());
            }
            
            return mdns;
        }

        static DSNMessage CreateNotificationMessage(Mdn mdn, TimeoutSettings settings)
        {
            var perMessage = new DSNPerMessage(settings.ProductName, mdn.MessageId);
            var perRecipient = new DSNPerRecipient(DSNStandard.DSNAction.Failed, DSNStandard.DSNStatus.Permanent
                                                   , DSNStandard.DSNStatus.NETWORK_EXPIRED_PROCESSED,
                                                   MailParser.ParseMailAddress(mdn.Recipient));
            //
            // The nature of Mdn storage in config store does not result in a list of perRecipients
            // If you would rather send one DSN with muliple recipients then one could write their own Job.
            //
            var notification = new DSN(perMessage, new List<DSNPerRecipient> { perRecipient });
            var sender = new MailAddress(mdn.Sender);
            var notificationMessage = new DSNMessage(sender.Address, new MailAddress("Postmaster@" + sender.Host).Address, notification);
            notificationMessage.AssignMessageID();
            notificationMessage.SubjectValue = string.Format("{0}:{1}", "Rejected", mdn.SubjectValue);
            notificationMessage.Timestamp();
            return notificationMessage;
        }

    }
}

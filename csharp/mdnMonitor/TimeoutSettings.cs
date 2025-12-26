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
using Quartz;

namespace Health.Direct.MdnMonitor
{
    /// <summary>
    /// <c>TimeoutSettings</c> represent the quartznet job-data-map name value store in the jobs.xml config file.
    /// </summary>
    public class TimeoutSettings : MdnSettings
    {
        const string BulkCountSetting = "BulkCount";
        const string ExpiredMinutesSetting = "ExpiredMinutes";
        const string PickupFolderSettings = "PickupFolder";

        private TimeSpan m_exiredMinutes;
        
        /// <summary>
        /// Create <c>TimeoutSettings</c> from Job context in the jobs.xml config file.
        /// </summary>
        /// <param name="context"></param>
        public TimeoutSettings(IJobExecutionContext context)
            : base()
        {
            Load(context);
        }

        /// <summary>
        /// Number of messages to process.
        /// Or number of records to query.
        /// </summary>
        public int BulkCount { get; set; }
        
        /// <summary>
        /// The number of minutes until a notification correlation is considered expired.
        /// </summary>
        public TimeSpan ExpiredMinutes
        {
            get { return m_exiredMinutes; }
        }
        /// <summary>
        /// Location of the pickup folder for message delivery
        /// </summary>
        public string PickupFolder { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ErrorCode { get; set; }

        
        private void Load(IJobExecutionContext context)
        {
            BulkCount = context.JobDetail.JobDataMap.GetInt(BulkCountSetting);

            int minutes = context.JobDetail.JobDataMap.GetInt(ExpiredMinutesSetting);
            m_exiredMinutes = TimeSpan.FromMinutes(minutes);

            PickupFolder = context.JobDetail.JobDataMap.GetString(PickupFolderSettings);

        }
    }
}

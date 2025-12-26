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
using System.Collections.Generic;
using Health.Direct.Common.Diagnostics;
using Health.Direct.Common.Extensions;
using Health.Direct.Config.Store;
using Quartz;

namespace Health.Direct.MdnMonitor
{
    
    ///<summary>
    /// An object holding timout conditions and execution code to act on those conditions.
    ///</summary>
    public abstract class Timeout
    {
        private ConfigStore m_store;
        private ILogger m_logger;

        /// <summary>
        /// Reference MDNManager for access to data store.
        /// </summary>
        protected MdnManager MDNManager { get; set; }

        
        /// <summary>
        /// Config Store 
        /// Configured in <c>Load</c>
        /// </summary>
        protected ConfigStore Store
        {
            get
            {
                return m_store;
            }
        }

        /// <summary>
        /// Logger
        /// Configured in <c>Load</c>.
        /// </summary>
        protected ILogger Logger
        {
            get { return m_logger; }
        }

        

        /// <summary>
        /// Get queued mdns that are considered expired based on type of <c>Timeout</c>
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected abstract IList<Mdn> ExpiredMdns(TimeoutSettings settings);

        /// <summary>
        /// Generate unique mime file name.
        /// </summary>
        /// <returns></returns>
        protected string UniqueFileName()
        {
            return StringExtensions.UniqueString() + ".eml";
        }

        /// <summary>
        /// Load applicatioin settings and job settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected TimeoutSettings Load(IJobExecutionContext context)
        {
            try
            {
                var settings = new TimeoutSettings(context);
                m_store = new ConfigStore(settings.ConnectionString, settings.QueryTimeout);
                m_logger = Log.For(this);
                
                MDNManager = new MdnManager(new ConfigStore(settings.ConnectionString));

                return settings;
            }
            catch (Exception e)
            {
                WriteToEventLog(e);
                var je = new JobExecutionException(e);
                je.UnscheduleAllTriggers = true;
                throw je;
            }
        }

        private static void WriteToEventLog(Exception ex)
        {
            const string source = "Health.Direct.MdnMonitor";

            EventLogHelper.WriteError(source, ex.Message);
            EventLogHelper.WriteError(source, ex.GetBaseException().ToString());
        }

        
    }
}
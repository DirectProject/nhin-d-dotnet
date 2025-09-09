/* 
 Copyright (c) 2010, Direct Project
 All rights reserved.

 Authors:
    Joe Shook	    jshook@kryptiq.com

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/

using System;
using System.IO;
using System.Linq;
using Health.Direct.Common.Mail;
using Health.Direct.Common.Mail.DSN;
using Health.Direct.Config.Store.Tests;
using Quartz;
using Xunit;

namespace Health.Direct.MdnMonitor.MdnMonitor.Tests
{
    public class TestTimeOut : ConfigStoreTestBase
    {
        const string PickupFolder = @"c:\inetpub\mailroot\testPickup";

        static TestTimeOut()
        {
            if (!Directory.Exists(PickupFolder))
            {
                Directory.CreateDirectory(PickupFolder);
            }
        }

        [Fact]
        public void TestProcessedTimeOutToDSNFail()
        {
            var target = CreateManager();
            InitMdnRecords();
            CleanMessages(PickupFolder);
            
            //timespan and max records set
            var mdns = target.GetExpiredProcessed(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(20, mdns.Count());

            var processedTimeout = new MdnProcessedTimeout();

            // > 10 minutes (nothing processed)
            var context = CreateProcessedJobExecutionContext(11, 10);
            processedTimeout.Execute(context).GetAwaiter().GetResult();

            mdns = target.GetExpiredProcessed(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(20, mdns.Count());

            // == 10 minutes (process 10)
            context = CreateProcessedJobExecutionContext(10, 10);
            processedTimeout.Execute(context).GetAwaiter().GetResult();

            mdns = target.GetExpiredProcessed(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(10, mdns.Count());

            var files = Directory.GetFiles(PickupFolder);
            Assert.Equal(10, files.Length);

            // again (process remaining 10)
            processedTimeout.Execute(context).GetAwaiter().GetResult();
            mdns = target.GetExpiredProcessed(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(0, mdns.Count());

            files = Directory.GetFiles(PickupFolder);
            Assert.Equal(20, files.Length);

            foreach (var file in files)
            {
                var loadedMessage = Message.Load(File.ReadAllText(file));
                Assert.True(loadedMessage.IsDSN());
                Assert.Equal("multipart/report", loadedMessage.ParsedContentType.MediaType);
                Assert.Equal("Rejected:To dispatch or not dispatch", loadedMessage.SubjectValue);
                var dsnActual = DSNParser.Parse(loadedMessage);
                Assert.Equal(DSNStandard.DSNAction.Failed, dsnActual.PerRecipient.First().Action);
                Assert.Equal("5.4.71", dsnActual.PerRecipient.First().Status);
            }
        }

        [Fact]
        public void TestDispatchedTimeOutToDSNFail()
        {
            var target = CreateManager();
            InitMdnRecords();
            CleanMessages(PickupFolder);

            var mdns = target.GetExpiredDispatched(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(10, mdns.Count());

            var dispatchedTimeout = new MdnDispatchedTimeout();

            // > 10 minutes (none)
            var context = CreateDispatchedJobExecutionContext(11, 5);
            dispatchedTimeout.Execute(context).GetAwaiter().GetResult();

            mdns = target.GetExpiredDispatched(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(10, mdns.Count());
            Assert.Empty(Directory.GetFiles(PickupFolder));

            // == 10 minutes (process 5)
            context = CreateDispatchedJobExecutionContext(10, 5);
            dispatchedTimeout.Execute(context).GetAwaiter().GetResult();

            mdns = target.GetExpiredDispatched(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(5, mdns.Count());

            var files = Directory.GetFiles(PickupFolder);
            Assert.Equal(5, files.Length);

            // again (process remaining 5)
            dispatchedTimeout.Execute(context).GetAwaiter().GetResult();
            mdns = target.GetExpiredDispatched(TimeSpan.FromMinutes(10), 40);
            Assert.Equal(0, mdns.Count());

            files = Directory.GetFiles(PickupFolder);
            Assert.Equal(10, files.Length);

            foreach (var file in files)
            {
                var loadedMessage = Message.Load(File.ReadAllText(file));
                Assert.True(loadedMessage.IsDSN());
                Assert.Equal("multipart/report", loadedMessage.ParsedContentType.MediaType);
                Assert.Equal("Rejected:To dispatch or not dispatch", loadedMessage.SubjectValue);
                var dsnActual = DSNParser.Parse(loadedMessage);
                Assert.Equal(DSNStandard.DSNAction.Failed, dsnActual.PerRecipient.First().Action);
                Assert.Equal("5.4.72", dsnActual.PerRecipient.First().Status);
            }
        }

        protected virtual IJobExecutionContext CreateProcessedJobExecutionContext(int minutes, int count)
        {
            var jobDetail = JobBuilder.Create<MdnProcessedTimeout>()
                                      .WithIdentity("processedTimeout", "tests")
                                      .Build();
            jobDetail.JobDataMap.Put("BulkCount", count);
            jobDetail.JobDataMap.Put("ExpiredMinutes", minutes);
            jobDetail.JobDataMap.Put("PickupFolder", PickupFolder);

            var trigger = TriggerBuilder.Create()
                                        .WithIdentity("processedTrigger", "tests")
                                        .StartNow()
                                        .Build();

            return new TestJobExecutionContext(jobDetail, trigger);
        }

        protected virtual IJobExecutionContext CreateDispatchedJobExecutionContext(int minutes, int count)
        {
            var jobDetail = JobBuilder.Create<MdnDispatchedTimeout>()
                                      .WithIdentity("dispatchedTimeout", "tests")
                                      .Build();
            jobDetail.JobDataMap.Put("BulkCount", count);
            jobDetail.JobDataMap.Put("ExpiredMinutes", minutes);
            jobDetail.JobDataMap.Put("PickupFolder", PickupFolder);

            var trigger = TriggerBuilder.Create()
                                        .WithIdentity("dispatchedTrigger", "tests")
                                        .StartNow()
                                        .Build();

            return new TestJobExecutionContext(jobDetail, trigger);
        }

        private void CleanMessages(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
        }
    }
}

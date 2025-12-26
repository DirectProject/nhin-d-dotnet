using System;
using Health.Direct.Config.Store;
using Health.Direct.Config.Store.Tests;
using Quartz;
using Xunit;

namespace Health.Direct.MdnMonitor.MdnMonitor.Tests
{
    public class TestCleanup : ConfigStoreTestBase
    {
        [Fact]
        public void TestDispatchedCleanup()
        {
            var target = CreateManager();
            InitOldMdnRecords();

            var dispositions = new CleanDispositions();

            var context = CreateCleanDispositionsJobExecutionContext(11);
            dispositions.Execute(context).GetAwaiter().GetResult();
            Assert.Equal(91, target.Count());

            context = CreateCleanDispositionsJobExecutionContext(9);
            dispositions.Execute(context).GetAwaiter().GetResult();
            Assert.Equal(51, target.Count());
        }

        [Fact]
        public void TestTimeoutCleanup()
        {
            var target = CreateManager();
            InitOldMdnRecords();

            var cleanupTimeout = new CleanTimeOut();
            Assert.Equal(91, target.Count());

            var context = CreateCleanTimeoutJobExecutionContext(11);
            cleanupTimeout.Execute(context).GetAwaiter().GetResult();
            Assert.Equal(91, target.Count());

            context = CreateCleanTimeoutJobExecutionContext(9);
            cleanupTimeout.Execute(context).GetAwaiter().GetResult();
            Assert.Equal(51, target.Count());
        }

        protected virtual IJobExecutionContext CreateCleanDispositionsJobExecutionContext(int days)
        {
            var jobDetail = JobBuilder.Create<CleanDispositions>()
                                      .WithIdentity("cleanDispositions", "tests")
                                      .Build();
            jobDetail.JobDataMap.Put("Days", days);

            var trigger = TriggerBuilder.Create()
                                        .WithIdentity("cleanDispositionsTrigger", "tests")
                                        .StartNow()
                                        .Build();

            return new TestJobExecutionContext(jobDetail, trigger);
        }

        protected virtual IJobExecutionContext CreateCleanTimeoutJobExecutionContext(int days)
        {
            var jobDetail = JobBuilder.Create<CleanTimeOut>()
                                      .WithIdentity("cleanTimeout", "tests")
                                      .Build();
            jobDetail.JobDataMap.Put("Days", days);

            var trigger = TriggerBuilder.Create()
                                        .WithIdentity("cleanTimeoutTrigger", "tests")
                                        .StartNow()
                                        .Build();

            return new TestJobExecutionContext(jobDetail, trigger);
        }
    }
}

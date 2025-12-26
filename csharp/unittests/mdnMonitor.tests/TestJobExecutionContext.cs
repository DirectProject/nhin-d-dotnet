using System;
using System.Threading;
using Quartz;

namespace Health.Direct.MdnMonitor.MdnMonitor.Tests
{
    /// <summary>
    /// Simple stub of IJobExecutionContext giving tests access to JobDetail.JobDataMap.
    /// Only members actually used by the tests are populated meaningfully.
    /// </summary>
    internal sealed class TestJobExecutionContext : IJobExecutionContext
    {
        public TestJobExecutionContext(IJobDetail jobDetail, ITrigger trigger)
        {
            JobDetail = jobDetail;
            Trigger = trigger;
            FireTimeUtc = DateTimeOffset.UtcNow;
            FireInstanceId = Guid.NewGuid().ToString();
            MergedJobDataMap = new JobDataMap();
            foreach (var k in jobDetail.JobDataMap.Keys)
            {
                MergedJobDataMap.Put(k, jobDetail.JobDataMap[k]);
            }
        }

        public void Put(object key, object objectValue)
        {
            throw new NotImplementedException();
        }

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public IScheduler Scheduler => null;
        public ITrigger Trigger { get; }
        public ICalendar Calendar => null;
        public bool Recovering => false;
        public TriggerKey RecoveringTriggerKey => null;
        public int RefireCount { get; }
        public IJobDetail JobDetail { get; }
        public IJob JobInstance => null;
        public DateTimeOffset FireTimeUtc { get; }
        public DateTimeOffset? ScheduledFireTimeUtc => FireTimeUtc;
        public DateTimeOffset? PreviousFireTimeUtc => null;
        public DateTimeOffset? NextFireTimeUtc => null;
        public string FireInstanceId { get; }
        public JobDataMap MergedJobDataMap { get; }
        public TimeSpan JobRunTime => TimeSpan.Zero;
        public object Result { get; set; }
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
using System;
using System.ServiceProcess;
using System.Threading;
using Quartz;
using Quartz.Impl;

namespace Health.Direct.Monitor.WinSrv
{
    //TODO: update to Async
    public partial class MdnMonitorWinSrv : ServiceBase
    {

        Diagnostics m_diagnostics;
        private ISchedulerFactory m_schedulerfactory;
        private IScheduler m_scheduler;

        public MdnMonitorWinSrv()
        {
            InitializeComponent();

            try
            {
                m_diagnostics = new Diagnostics(this);
            }
            catch (Exception ex)
            {
                Diagnostics.WriteEventLog(ex);
                throw;
            }
        }

        /// <summary>
        /// method to initialize fields utilized by the service
        /// </summary>
        private void InitializeService()
        {
            m_diagnostics.ServiceInitializing();

            m_schedulerfactory = new StdSchedulerFactory();
            // Quartz 3.x: GetScheduler() is async (Task<IScheduler>)
            m_scheduler = m_schedulerfactory.GetScheduler().GetAwaiter().GetResult();

            m_diagnostics.ServiceInitializingComplete();
        }

        public void StartService(string[] args)
        {
            try
            {
                InitializeService();

                m_diagnostics.ServerStarting();

                // Quartz 3.x: Start() is async
                m_scheduler.Start().GetAwaiter().GetResult();
                try
                {
                    Thread.Sleep(3000);
                }
                catch (ThreadInterruptedException)
                {
                }

                m_diagnostics.ServerStarted();
            }
            catch (Exception ex)
            {
                Diagnostics.WriteEventLog(ex);
                throw;
            }
        }
        public void StopService()
        {
            try
            {
                m_diagnostics.ServerStopping();

                // Quartz 3.x: Shutdown(waitForJobsToComplete) is async
                m_scheduler.Shutdown(true).GetAwaiter().GetResult();

                m_diagnostics.ServerStopped();
            }
            catch (Exception ex)
            {
                Diagnostics.WriteEventLog(ex);
                throw;
            }
        }

        protected override void OnStart(string[] args)
        {
            StartService(args);
        }

        protected override void OnStop()
        {
            StopService();
        }
    }
}

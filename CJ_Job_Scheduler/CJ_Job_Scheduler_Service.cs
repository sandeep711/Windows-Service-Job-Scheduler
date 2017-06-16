using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using CJ_Job_Scheduler.App_Code.Helper;
using CJ_Job_Scheduler.App_Code.Bll;

namespace CJ_Job_Scheduler
{
    partial class CJ_Job_Scheduler_Service : ServiceBase
    {
        System.Timers.Timer cjJobSchedulerTimer = null;


        bool isError = false;

        bool singleJobError = false;


        public CJ_Job_Scheduler_Service()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(Constants.CJ_Job_Scheduler_Log_Source))
            {
                EventLog.CreateEventSource(Constants.CJ_Job_Scheduler_Log_Source, Constants.CJ_Job_Scheduler_Log);
            }

            eventLog.Source = Constants.CJ_Job_Scheduler_Log_Source;
            eventLog.Log = Constants.CJ_Job_Scheduler_Log;
        }

        protected override void OnStart(string[] args)
        {
            isError = false;

            Util.CJLog("OnStart()", "Service started.", EventLogEntryType.Information);

            cjJobSchedulerTimer = new System.Timers.Timer(Retrieve.GetHeartbeatInterval());

            cjJobSchedulerTimer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

            cjJobSchedulerTimer.Start();

            Util.SendEmail("Started: " + DateTime.Now.ToString(), "Started");


        }

        protected override void OnStop()
        {
            string msg = string.Empty;

            Util.TerminateCurrentlyRunningProcesses(ref msg); //kill running processes

            Util.CJLog("OnStop()", "Service stopped." + Environment.NewLine + "Terminated: " + msg, EventLogEntryType.Information);

            cjJobSchedulerTimer.Stop();
            cjJobSchedulerTimer = null;

            Util.SendEmail("Stopped: " + DateTime.Now.ToString() + Environment.NewLine + "Terminated: " + msg , "Stopped");

        }

        protected override void OnPause()
        {
            base.OnPause();
            Util.CJLog("OnPause()", "Service paused.", EventLogEntryType.Information);
            cjJobSchedulerTimer.Stop();

            Util.SendEmail("Paused: " + DateTime.Now.ToString(), "Paused");

        }

        protected override void OnContinue()
        {
            base.OnContinue();
            Util.CJLog("OnContinue()", "Service resumed.", EventLogEntryType.Information);
            cjJobSchedulerTimer.Start();

            isError = false;

            Util.SendEmail("Resumed: " + DateTime.Now.ToString(), "Resumed");
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            Util.CJLog("OnShutdown()", "Service shutdown.", EventLogEntryType.Information);
            cjJobSchedulerTimer.Stop();

            Util.SendEmail("Shutdown: " + DateTime.Now.ToString(), "Shutdown");

        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer cjJobSchedulerTimer = (System.Timers.Timer)sender;

            singleJobError = false;
            string message = string.Empty;


            try
            {

                //If the connection string has an issue, it will be trapped in the next line
                //what if the email has issue?? the web UI can assist to see the LatestHeatbeat
                //parameter.Update(ParameterNameEnum.SchedulerLatestHeartbeatDate, DateTime.Now.ToString());

                //if (DateTime.Now.ToString("HH:mm") == "20:00")
                //{
                //Util.SendEmail("Heartbeat: " + DateTime.Now.ToString(), "Heartbeat");
                Util.CJLog("timer_Elapsed()", "Scheduler Service - Heartbeat.", EventLogEntryType.Information);
                //}

                cjJobSchedulerTimer.Stop(); // stop for awhile to process the jobs

                InterfaceJobSchedule js = new InterfaceJobSchedule();
                js.ExecuteJobs(ref singleJobError, ref message);

            }
            catch (Exception ex)
            {
                Util.CJLog("timer_Elapsed()", ex.Message, EventLogEntryType.Error);

                Util.SendEmail("Occured: " + DateTime.Now.ToString() +
                    Environment.NewLine + "Exception: " + ex.Message, "EXCEPTION");

                isError = true;
            }
            finally
            {
                if (!isError) // isError is set to true when there is an exception (e.g. connection string issue)
                {
                    cjJobSchedulerTimer.Start();
                }
                else
                {
                    ServiceController c = new ServiceController("CJ_Job_Scheduler_Service");
                    c.Stop();
                }
            }
        }


       





    }
}

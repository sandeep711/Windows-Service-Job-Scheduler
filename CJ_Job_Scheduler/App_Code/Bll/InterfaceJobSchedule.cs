using System;
using CJ_Job_Scheduler.App_Code.Helper;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Text;



/**********************
 *  Windows Service Scheduler is responsible for triggering Daily, Monthly and Yearly (Mass, Adhoc, Demise etc)
 *  Will Trigger based on the Schedule Date and Time with a buffer late run of 5 mins 
 *    - e.g. if the current time 2:05pm (or less) and the schedule was at 2pm, it will pick up the job to run. 
 *    - As a result, the scheduler assumes that it can check (heartbeat) at least every 5 minutes.
 *    
 *  A job on RUNMODE = Schedule will not run if it had run on the same date (meaning LastExecutedDate is the same date)
 *  Threfore, in order to run on the same day again, RUNMODE has to be set to 'ForceStart'. It will run despite it had run before.
 *  
 * .exe or winform exeuctable will be terminated before calling the same exe is called again. This is to ensure no multiple instance of exe is run.
 * Note that the .exe names have been hardcoded, so if the names changed, the UTIL.CS class, need to be changed.   
 *              string[] processNameArray = new string[] 
                { 
                    "CPSII_CalculationMassRun", 
                    "CPSII_CalculationBatch", 
                    "Cpsii.Task.WinForms"
                }; 
 *  For exe and winform, the sheduler heartbeat need to continue, therefore, WAITUNTIL end is set to false, this is to make sure that heartbeat continues without waiting until
 *  the exe is completed running.
 * ******************/

namespace CJ_Job_Scheduler.App_Code.Bll
{

    public static class Extensions
    {
        public static string ToCSV(this DataTable table)
        {
            var result = new StringBuilder();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                result.Append(table.Columns[i].ColumnName);
                result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
            }

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(row[i].ToString());
                    result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
                }
            }

            return result.ToString();
        }
    }
    
    public class InterfaceJobSchedule
    {

        static string path = Retrieve.GetScheduleFile();
        static string pathDetail = Retrieve.GetScheduleDetailFile();

        static string fullPath = Retrieve.GetApplicationPath() + "\\" + path;
        static string fullPathDetail = Retrieve.GetApplicationPath() + "\\" + pathDetail;
        static string separatorChar = ",";


        private static DataTable GetData(string filename, string separatorChar, out List<string> errors)
        {
            errors = new List<string>();
            var table = new DataTable("StringLocalization");
            //using (var sr = new StreamReader(filename, Encoding.Default))
            using (var sr = new StreamReader(File.OpenRead(filename), Encoding.Default))
            {
                string line;
                var i = 0;
                while (sr.Peek() >= 0)
                {
                    try
                    {
                        line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        var values = line.Split(new[] { separatorChar }, StringSplitOptions.None);
                        var row = table.NewRow();
                        for (var colNum = 0; colNum < values.Length; colNum++)
                        {
                            var value = values[colNum];
                            if (i == 0)
                            {
                                table.Columns.Add(value, typeof(String));
                            }
                            else
                            {
                                row[table.Columns[colNum]] = value;
                            }
                        }
                        if (i != 0) table.Rows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                    i++;
                }
            }
            return table;
        }

        private static DataTable GetDataDetail(string filename, string separatorChar, out List<string> errors)
        {
            errors = new List<string>();
            var table = new DataTable("StringLocalization");
            //using (var sr = new StreamReader(filename, Encoding.Default))
            using (var sr = new StreamReader(File.OpenRead(filename), Encoding.Default))
            {
                string line;
                var i = 0;
                while (sr.Peek() >= 0)
                {
                    try
                    {
                        line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        var values = line.Split(new[] { separatorChar }, StringSplitOptions.None);
                        var row = table.NewRow();
                        for (var colNum = 0; colNum < values.Length; colNum++)
                        {
                            var value = values[colNum];
                            if (i == 0)
                            {
                                table.Columns.Add(value, typeof(String));
                            }
                            else
                            {
                                row[table.Columns[colNum]] = value;
                            }
                        }
                        if (i != 0) table.Rows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                    }
                    i++;
                }
            }
            return table;
        }

        public static DataTable GetAllJobs(ref bool singleJobError)
        {
            List<string> errors = new List<string>();
            try
            {
                return GetData(fullPath, separatorChar, out errors);
            }
            catch (Exception ex)
            {
                Util.CJLog("GetAllJobs", "Error: " + ex.Message + ", stack: " + ex.StackTrace + ", inner: +" + ex.InnerException, EventLogEntryType.Error);
                Util.SendEmail(ex.Message, "Error GetAllJobs");
                singleJobError = true;
                errors = null;
                return null;
            }
        }

        public static DataTable GetAllJobsDetail(ref bool singleJobError)
        {
            List<string> errors = new List<string>();
            try
            {
                return GetDataDetail(fullPathDetail, separatorChar, out errors);
            }
            catch (Exception ex)
            {
                Util.CJLog("GetAllJobsDetail", "Error: " + ex.Message + ", stack: " + ex.StackTrace + ", inner: +" + ex.InnerException, EventLogEntryType.Error);
                Util.SendEmail(ex.Message, "Error GetAllJobsDetail");
                singleJobError = true;
                errors = null;
                return null;
            }
        }

        private bool UpdateJobDetails(string detailId)
        {

            bool singleJobError = false;
            DataTable dt = GetAllJobsDetail(ref singleJobError);
            var rows = dt.Select("DetailId='" + detailId.Trim() + "'");

            foreach (var r in rows)
            {
                r["LastExecutedDate"] = DateTime.Now.ToString();
                r["RunMode"] = RunMode.Schedule.ToString();
            }

            int rowsAffected = rows.Length;

            if (rows.Length == 0)
            {
                //insert a record
                dt.Rows.Add(detailId, RunMode.Schedule.ToString(), DateTime.Now.ToString());
            }


            try
            {
                CreateCSVFile(dt);
            }
            catch (Exception ex)
            {

            }

            return rowsAffected == 1;
        }

        public void CreateCSVFile(DataTable dt)
        {
            var bytes = Encoding.GetEncoding("utf-8").GetBytes(dt.ToCSV()); //iso-8859-1
            File.WriteAllBytes(fullPathDetail, bytes);
        }

        
        public bool ExecuteJobs(ref bool singleJobError, ref string message)
        {
            DataTable dt = null;
            DataTable dtDetail = null;

            if (!singleJobError)
                dt = GetAllJobs(ref singleJobError);
            else
                return false;

            if (!singleJobError)
                dtDetail = GetAllJobsDetail(ref singleJobError);
            else
                return false;

            //if (!singleJobError)
            //    ProcessDailyJobs(dt, ref singleJobError);
            //else
            //    return false;

            //if (!singleJobError)
            //    ProcessMonthlyJobs(dt, ref singleJobError);
            //else
            //    return false;

            //if (!singleJobError)
            //    ProcessYearlyAndMassAndAdhocJobs(dt, ref singleJobError); //yearly job comprise of yearly, ad-hoc and mass
            //else
            //    return false;



            if (!singleJobError)
                TestProcessDailyJobs(dt, dtDetail, ref singleJobError, ref message);
            else
                return false;


            return true;
        }

        private bool RunNow(DateTime schedule, DateTime now, string frequency)
        {
            const int BUFFER_IN_MINUTES = 5;

            if (frequency == FrequencyType.Daily.ToString())
            {
                //compare the exact time, late-run within 5min Buffer
                if (schedule.Hour == now.Hour)
                {
                    if (now.Minute - schedule.Minute <= BUFFER_IN_MINUTES && (now.Minute - schedule.Minute) >= 0)
                    {
                        return true;
                    }
                }
            }
            else if (frequency == FrequencyType.Monthly.ToString())
            {
                if (schedule.Day == now.Day)
                {
                    if (schedule.Hour == now.Hour)
                    {
                        if (now.Minute - schedule.Minute <= BUFFER_IN_MINUTES && (now.Minute - schedule.Minute) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            else if (frequency == FrequencyType.Yearly.ToString())
            {
                if (schedule.Month == now.Month)
                {
                    if (schedule.Day == now.Day)
                    {
                        if (schedule.Hour == now.Hour)
                        {
                            if (now.Minute - schedule.Minute <= BUFFER_IN_MINUTES && (now.Minute - schedule.Minute) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }


            return false;
        }
                
        private bool TestProcessDailyJobs(DataTable jobTable, DataTable jobTableDetail, ref bool singleJobError, ref string message)
        {
            //var t = from o in jobTable.AsEnumerable()
            //        where o.Field<string>("Frequency").Trim() == FrequencyType.Daily.ToString()
            //        select o;


            var t = from o in jobTable.AsEnumerable()
                    select o;

            foreach (var job in t)
            {
                DateTime? schedule = null;

                if (!string.IsNullOrEmpty(job.Field<string>("Schedule")))
                {
                    schedule = Convert.ToDateTime(job.Field<string>("Schedule"));
                }

                string frequency = job.Field<string>("Frequency").Trim();
                string jobPath = job.Field<string>("JobPath").Trim();
                string jobName = job.Field<string>("JobName").Trim();
                string jobDetailId = job.Field<string>("DetailId").Trim();


                var vDetail = from o in jobTableDetail.AsEnumerable()
                              where o.Field<string>("DetailId").Trim() == jobDetailId
                              select o;



                string runMode = string.Empty;
                DateTime? lastExecutedDate = null;

                foreach (var d in vDetail)
                {
                    runMode = d.Field<string>("RunMode").Trim().ToLower();

                    if (!string.IsNullOrEmpty(d.Field<string>("LastExecutedDate")))
                    {
                        lastExecutedDate = Convert.ToDateTime(d.Field<string>("LastExecutedDate"));
                    }

                }

                if (runMode == RunMode.Schedule.ToString().ToLower())
                {
                    DateTime now = DateTime.Now;

                    if (lastExecutedDate.HasValue)
                    {
                        if (lastExecutedDate.Value.Date != now.Date) //if aleady exucuted, nothing to run, if not need to check with the schedule
                        {
                            //checck if it the correct time. (maybe add a buffer, 5 mins)
                            if (RunNow((DateTime)schedule, now, frequency))
                            {
                                UpdateJobDetails(jobDetailId);
                                RunExecutableFile(jobPath, frequency, jobName, ref singleJobError, false, ref message);
                            }
                        }

                    }
                    else
                    {
                        if (RunNow((DateTime)schedule, now, frequency))
                        {
                            UpdateJobDetails(jobDetailId);
                            RunExecutableFile(jobPath, frequency, jobName, ref singleJobError, false, ref message);
                        }

                    }
                }
                else if (runMode == RunMode.ForceStart.ToString().ToLower())
                {
                    UpdateJobDetails(jobDetailId);
                    RunExecutableFile(jobPath, frequency, jobName, ref singleJobError, false, ref message);
                }
                else if (string.IsNullOrEmpty(runMode))
                {
                    UpdateJobDetails(jobDetailId);
                    RunExecutableFile(jobPath, frequency, jobName, ref singleJobError, false, ref message);
                }

            }

            return true;
        }


        //private bool ProcessDailyJobs(Dal.InterfaceJobSchedule.InterfaceJobScheduleDataTable jobTable, ref bool singleJobError)
        //{



        //    var t = from o in jobTable
        //            where o.Frequency.Trim() == FrequencyType.Daily.ToString()
        //            select o;

        //    foreach (var job in t)
        //    {

        //        if (job.RunMode.Trim().ToLower() == RunMode.Schedule.ToString().ToLower())
        //        {

        //            DateTime now = DateTime.Now;

        //            if (!job.IsLastExecutedDateNull())
        //            {
        //                if (job.LastExecutedDate.Date != now.Date) //if aleady exucuted, nothing to run, if not need to check with the schedule
        //                {
        //                    //checck if it the correct time. (maybe add a buffer, 5 mins)
        //                    if (RunNow(job.Schedule, now, job.Frequency))
        //                    {
        //                        RunExecutableFile(job.JobPath, "ProcessDailyJobs", job.JobName, ref singleJobError, false);
        //                        UpdateLastJobExceutedDateAndTime(job.DetailId);
        //                    }
        //                }

        //            }
        //            else
        //            {
        //                if (RunNow(job.Schedule, now, job.Frequency))
        //                {
        //                    RunExecutableFile(job.JobPath, "ProcessDailyJobs", job.JobName, ref singleJobError, false);
        //                    UpdateLastJobExceutedDateAndTime(job.DetailId);
        //                }

        //            }
        //        }
        //        else if (job.RunMode.Trim().ToLower() == RunMode.ForceStart.ToString().ToLower())
        //        {
        //            RunExecutableFile(job.JobPath, "ProcessDailyJobs", job.JobName, ref singleJobError, false);
        //            UpdateLastJobExceutedDateAndTime(job.DetailId);
        //            UpdateJobRunMode(job.JobId, RunMode.Schedule); //job will not run again on same day, need to force-start in order to run on same day
        //        }
        //    }

        //    return true;
        //}



        //private bool ProcessMonthlyJobs(Dal.InterfaceJobSchedule.InterfaceJobScheduleDataTable jobTable, ref bool singleJobError)
        //{


        //    var t = from o in jobTable
        //            where o.Frequency.Trim() == FrequencyType.Monthly.ToString()
        //            select o;

        //    foreach (var job in t)
        //    {
        //        if (job.RunMode.Trim().ToLower() == RunMode.Schedule.ToString().ToLower())
        //        {
        //            DateTime now = DateTime.Now;

        //            int jobScheduleDay = job.Schedule.Day;
        //            int nowDay = now.Day;

        //            if (jobScheduleDay == nowDay) // Monthly
        //            {
        //                if (!job.IsLastExecutedDateNull())
        //                {

        //                    //Check if it ran previously on same day
        //                    if (job.LastExecutedDate.Date != now.Date)
        //                    {
        //                        //checck if it the correct time. (maybe add a buffer, 5 mins)
        //                        if (RunNow(job.Schedule, now, job.Frequency))
        //                        {
        //                            RunExecutableFile(job.JobPath, "ProcessMonthlyJobs", job.JobName, ref singleJobError, false);
        //                            UpdateLastJobExceutedDateAndTime(job.DetailId);
        //                        }
        //                    }

        //                }
        //                else
        //                {
        //                    //checck if it the correct time. (maybe add a buffer, 5 mins)
        //                    if (RunNow(job.Schedule, now, job.Frequency))
        //                    {
        //                        RunExecutableFile(job.JobPath, "ProcessMonthlyJobs", job.JobName, ref singleJobError, false);
        //                        UpdateLastJobExceutedDateAndTime(job.DetailId);
        //                    }
        //                }
        //            }
        //        }
        //        else if (job.RunMode.Trim().ToLower() == RunMode.ForceStart.ToString().ToLower())
        //        {
        //            RunExecutableFile(job.JobPath, "ProcessMonthlyJobs", job.JobName, ref singleJobError, false);
        //            UpdateLastJobExceutedDateAndTime(job.DetailId);
        //            UpdateJobRunMode(job.JobId, RunMode.Schedule); //job will not run again on same day, need to force-start in order to run on same day
        //        }
            
        //    }

        //    return true;
        //}



        //private bool ProcessYearlyAndMassAndAdhocJobs(Dal.InterfaceJobSchedule.InterfaceJobScheduleDataTable jobTable, ref bool singleJobError)
        //{
        //    var t = from o in jobTable
        //            where o.Frequency.Trim() == FrequencyType.Yearly.ToString()
        //            select o;

        //    foreach (var job in t)
        //    {
        //        if (job.RunMode.Trim().ToLower() == RunMode.Schedule.ToString().ToLower())
        //        {
        //            DateTime now = DateTime.Now;

        //            if (!job.IsLastExecutedDateNull())
        //            {
        //                //Check if it ran previously on same date
        //                if (job.LastExecutedDate.Date != now.Date)
        //                {
        //                    if (RunNow(job.Schedule, now, job.Frequency))
        //                    {
        //                        RunExecutableFile(job.JobPath, "ProcessYearlyJobs", job.JobName, ref singleJobError, false);
        //                        UpdateLastJobExceutedDateAndTime(job.DetailId);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                if (RunNow(job.Schedule, now, job.Frequency))
        //                {
        //                    RunExecutableFile(job.JobPath, "ProcessYearlyJobs", job.JobName, ref singleJobError, false);
        //                    UpdateLastJobExceutedDateAndTime(job.DetailId);
        //                }
        //            }

        //        }
        //        else if (job.RunMode.Trim().ToLower() == RunMode.ForceStart.ToString().ToLower())
        //        {
        //            RunExecutableFile(job.JobPath, "ProcessYearlyJobs", job.JobName, ref singleJobError, false);
        //            UpdateLastJobExceutedDateAndTime(job.DetailId);
        //            UpdateJobRunMode(job.JobId, RunMode.Schedule); //job will not run again on same day, need to force-start in order to run on same day
        //        }
        //    }
        //    return true;
        //}




        private bool RunExecutableFile(string path, string functionName, string jobName, ref bool singleJobError, bool waitUntilEnd, ref string message)
        {
            string msg = string.Empty;
            string innerMsg = string.Empty;
            DateTime start = DateTime.Now;
            int exitCode = -1;

            //Util.CJLog(functionName, jobName + " started", EventLogEntryType.Warning);

            msg += "Type: " + functionName;
            msg += Environment.NewLine + "Job: " + jobName;
            msg += Environment.NewLine + "Path: " + path;
            msg += Environment.NewLine + "Start: " + DateTime.Now;



            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string fullPath = Retrieve.GetApplicationPath() + "\\" + path;


                    //TODO: commented out for testing BAT files
                    //if (Path.GetExtension(fullPath.Trim()) == ".exe" || fullPath.Contains("WinForms"))
                    //    waitUntilEnd = false;
                    //else
                    //    waitUntilEnd = true;


                    //if (!fullPath.Contains("WinForms"))
                    Util.TerminateIndividualProcess(ref msg, Path.GetFileNameWithoutExtension(fullPath));


                    Util.ExecuteCommandEXE(fullPath.Trim(), ref innerMsg, waitUntilEnd, ref exitCode);

                    msg += Environment.NewLine + "Result: " + innerMsg;

                    if (exitCode <= 0)
                    {
                        msg += Environment.NewLine + "Status: Job Trigger";

                        Util.CJLog(functionName, msg, EventLogEntryType.Warning);
                        Util.SendEmail(msg, "Trigger Batch Job");
                        return true;
                    }
                    else if (exitCode > 0)
                    {

                        msg += Environment.NewLine + "Status: Job Failed upon Trigger";
                        //msg += Environment.NewLine + "Ended: " + DateTime.Now;

                        Util.CJLog(functionName, msg, EventLogEntryType.Error);
                        Util.SendEmail(msg, "FAILED Trigger");
                        singleJobError = true;

                        return false;
                    }
                }
                else
                {
                    if (jobName.Contains("Alive"))
                    {
                        msg += Environment.NewLine + "Status: Alive Notification";
                        Util.CJLog(functionName, msg, EventLogEntryType.Information);
                        Util.SendEmail(msg, "Alive");
                    }
                    else
                    {
                        msg += Environment.NewLine + "Status: Job Skipped";
                        Util.CJLog(functionName, msg, EventLogEntryType.Warning);
                        Util.SendEmail(msg, "SKIPPED Trigger");
                    }
                        return true;
                }
                return false;

            }
            catch (Exception ex)
            {

                msg += Environment.NewLine + "Result:" + ex.Message + Environment.NewLine + ex.InnerException + Environment.NewLine + ex.StackTrace;
                msg += Environment.NewLine + "Status: Error running RunExecutableFile";
                //msg += Environment.NewLine + "Ended: " + DateTime.Now;


                Util.CJLog(functionName, msg, EventLogEntryType.Error);
                Util.SendEmail(msg, "FAILED Job Start");

                singleJobError = true;

                return false;
            }
            //finally
            //{
            //    Util.CJLog(functionName, jobName + " ended", EventLogEntryType.Warning);
            //}
        }




        private bool RunExecutableFileNoWait(string path, string functionName, string jobName, ref bool singleJobError, bool waitUntilEnd)
        {
            string msg = string.Empty;
            string innerMsg = string.Empty;
            DateTime start = DateTime.Now;


            //Util.CJLog(functionName, jobName + " started", EventLogEntryType.Warning);

            msg += "Type: NO_WAIT" + functionName;
            msg += Environment.NewLine + "Job: " + jobName;
            msg += Environment.NewLine + "Path: " + path;
            msg += Environment.NewLine + "Start: " + DateTime.Now;



            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string fullPath = Retrieve.GetApplicationPath() + "\\" + path;

                    if (Path.GetExtension(fullPath.Trim()) == ".exe" || fullPath.Contains("WinForms"))
                        waitUntilEnd = false;
                    else
                        waitUntilEnd = true;


                    //msg += Environment.NewLine + "Name: " + Path.GetFileName(fullPath).ToString();

                    //if (!fullPath.Contains("WinForms"))
                    Util.TerminateIndividualProcess(ref msg, Path.GetFileNameWithoutExtension(fullPath));


                    int errorCode = Util.ExecuteCommandNoWait(fullPath.Trim(), ref innerMsg, waitUntilEnd);

                    msg += Environment.NewLine + "Result:" + innerMsg;

                    if (errorCode == 0)
                    {
                        msg += Environment.NewLine + "Status: Job Trigger";

                        Util.CJLog(functionName, msg, EventLogEntryType.Warning);
                        Util.SendEmail(msg, "Trigger Batch Job");
                        return true;
                    }
                    else
                    {

                        msg += Environment.NewLine + "Status: Job Failed upon Trigger";
                        //msg += Environment.NewLine + "Ended: " + DateTime.Now;

                        Util.CJLog(functionName, msg, EventLogEntryType.Error);
                        Util.SendEmail(msg, "FAILED Trigger");
                        singleJobError = true;

                        return false;
                    }
                }
                else
                {
                    if (jobName.Contains("Alive"))
                    {
                        msg += Environment.NewLine + "Status: Alive Notification";
                        Util.CJLog(functionName, msg, EventLogEntryType.Information);
                        Util.SendEmail(msg, "Alive");
                    }
                    else
                    {
                        msg += Environment.NewLine + "Status: Job Skipped";
                        Util.CJLog(functionName, msg, EventLogEntryType.Warning);
                        Util.SendEmail(msg, "SKIPPED Trigger");
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {

                msg += Environment.NewLine + "Result:" + ex.Message + Environment.NewLine + ex.InnerException + Environment.NewLine + ex.StackTrace;
                msg += Environment.NewLine + "Status: Error running RunExecutableFile";
                //msg += Environment.NewLine + "Ended: " + DateTime.Now;


                Util.CJLog(functionName, msg, EventLogEntryType.Error);
                Util.SendEmail(msg, "FAILED Job Start");

                singleJobError = true;

                return false;
            }
            //finally
            //{
            //    Util.CJLog(functionName, jobName + " ended", EventLogEntryType.Warning);
            //}
        }


        private bool RunExecutableFileWaitTime(string path, string functionName, string jobName, ref bool singleJobError, bool waitUntilEnd)
        {
            string msg = string.Empty;
            string innerMsg = string.Empty;
            DateTime start = DateTime.Now;


            //Util.CJLog(functionName, jobName + " started", EventLogEntryType.Warning);

            msg += "Type: WAIT_TIME" + functionName;
            msg += Environment.NewLine + "Job: " + jobName;
            msg += Environment.NewLine + "Path: " + path;
            msg += Environment.NewLine + "Start: " + DateTime.Now;



            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string fullPath = Retrieve.GetApplicationPath() + "\\" + path;

                    if (Path.GetExtension(fullPath.Trim()) == ".exe" || fullPath.Contains("WinForms"))
                        waitUntilEnd = false;
                    else
                        waitUntilEnd = true;


                    //msg += Environment.NewLine + "Name: " + Path.GetFileName(fullPath).ToString();

                    //if (!fullPath.Contains("WinForms"))
                    Util.TerminateIndividualProcess(ref msg, Path.GetFileNameWithoutExtension(fullPath));


                    int errorCode = Util.ExecuteCommandWaitTime(fullPath.Trim(), ref innerMsg, waitUntilEnd);

                    msg += Environment.NewLine + "Result:" + innerMsg;

                    if (errorCode == 0)
                    {
                        msg += Environment.NewLine + "Status: Job Trigger";

                        Util.CJLog(functionName, msg, EventLogEntryType.Warning);
                        Util.SendEmail(msg, "Trigger Batch Job");
                        return true;
                    }
                    else
                    {

                        msg += Environment.NewLine + "Status: Job Failed upon Trigger";
                        //msg += Environment.NewLine + "Ended: " + DateTime.Now;

                        Util.CJLog(functionName, msg, EventLogEntryType.Error);
                        Util.SendEmail(msg, "FAILED Trigger");
                        singleJobError = true;

                        return false;
                    }
                }
                else
                {
                    if (jobName.Contains("Alive"))
                    {
                        msg += Environment.NewLine + "Status: Alive Notification";
                        Util.CJLog(functionName, msg, EventLogEntryType.Information);
                        Util.SendEmail(msg, "Alive");
                    }
                    else
                    {
                        msg += Environment.NewLine + "Status: Job Skipped";
                        Util.CJLog(functionName, msg, EventLogEntryType.Warning);
                        Util.SendEmail(msg, "SKIPPED Trigger");
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {

                msg += Environment.NewLine + "Result:" + ex.Message + Environment.NewLine + ex.InnerException + Environment.NewLine + ex.StackTrace;
                msg += Environment.NewLine + "Status: Error running RunExecutableFile";
                //msg += Environment.NewLine + "Ended: " + DateTime.Now;


                Util.CJLog(functionName, msg, EventLogEntryType.Error);
                Util.SendEmail(msg, "FAILED Job Start");

                singleJobError = true;

                return false;
            }
            //finally
            //{
            //    Util.CJLog(functionName, jobName + " ended", EventLogEntryType.Warning);
            //}
        }



      



    }
}

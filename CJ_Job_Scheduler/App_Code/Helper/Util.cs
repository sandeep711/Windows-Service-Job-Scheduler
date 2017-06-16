using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Data;
using CJ_Job_Scheduler.App_Code.Bll;
using DWMS_OCR.App_Code.Helper;

namespace CJ_Job_Scheduler.App_Code.Helper
{
    class Util
    {
        public static Process process;


        public static void CJLog(string functionName, string message, EventLogEntryType eventType)
        {
            Log(functionName, message, eventType, Constants.CJ_Job_Scheduler_Log_Source, Constants.CJ_Job_Scheduler_Log);
        }

        public static void TerminateCurrentlyRunningProcesses(ref string msg)
        {
            try
            {
                string[] processNameArray = new string[] 
                { 
                    "CPSII_CalculationMassRun", 
                    "CPSII_CalculationBatch", 
                    "CPSII_CalculationDemise", 
                    "CPSII_CalculationForfeiture", 
                    "CPSII_CalculationAccUpdate", 
                    "Cpsii.Task.WinForms"
                };

                foreach (string processName in processNameArray)
                {
                    TerminateIndividualProcess(ref msg, processName);
                }

                if (string.IsNullOrEmpty(msg))
                    msg += Environment.NewLine + "No running proceeses to terminate.";

            }
            catch (Exception ex)
            {
                msg += "Error encountered while terminating the ExecuteCommand "
                    + Environment.NewLine + " Source: " + ex.Source
                    + Environment.NewLine + " Message: " + ex.Message
                    + Environment.NewLine + " Stack Trace: " + ex.StackTrace
                    + Environment.NewLine + " Inner Exception: " + ex.InnerException;

            }
        }


        public static void TerminateIndividualProcess(ref string msg, string processName)
        {
            try
            {

                Process[] processes = Process.GetProcesses();
                for (int i = 0; i < processes.Count(); i++)
                {
                    //Any EXE's need to be killed
                    //TODO: do no change the exe names, the names are hard-coded, to kill the process
                    if (processes[i].ProcessName.Contains(processName))
                    {
                        processes[i].Kill();
                        msg += Environment.NewLine + "*** NOTE **** " + processName;
                        msg += Environment.NewLine + "*** Terminated the current instance of " + processName + " in order to proceed with this action! ****";
                        msg += Environment.NewLine + "*************************************";


                    }

                }

            }
            catch (Exception ex)
            {
                msg += "Error encountered while terminating the TerminateProcess "
                    + Environment.NewLine + " Source: " + ex.Source
                    + Environment.NewLine + " Message: " + ex.Message
                    + Environment.NewLine + " Stack Trace: " + ex.StackTrace
                    + Environment.NewLine + " Inner Exception: " + ex.InnerException;

            }
        }




        public static int ExecuteCommandEXE(string fullPath, ref string msg, bool waitUntilEnd, ref int exitCode)
        {
            try
            {
                ProcessStartInfo processInfo;

                processInfo = new ProcessStartInfo("cmd.exe", "/c " + fullPath);
                //processInfo = new ProcessStartInfo(command);

                         
                //processInfo.CreateNoWindow = true;
                //processInfo.UseShellExecute = false;
                //processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                
                // *** Redirect the output ***
                //processInfo.RedirectStandardError = true;
                //processInfo.RedirectStandardOutput = true;

                process = Process.Start(processInfo);

                if (waitUntilEnd)
                {
                    process.WaitForExit();


                    // *** Read the streams ***
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    exitCode = process.ExitCode;

                    msg += Environment.NewLine + "Output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output);
                    msg += Environment.NewLine + "Return msg>>" + (String.IsNullOrEmpty(error) ? "(none)" : error);
                    msg += Environment.NewLine + "ExitCode: " + exitCode.ToString();

                    process.Close();

                    if (!String.IsNullOrEmpty(error))
                        return 1;

                }
                else
                {
                    msg += " Scheduler is unable to determine the job execution results.";

                    exitCode = -1;

                    //process.WaitForExit();
                    
                    process.Close();
                    //process.Kill();


                    return 0;

                }

                return 0;

            }
            catch (Exception ex)
            {
                msg += "Error encountered while triggering the ExecuteCommandEXE "
                        + Environment.NewLine + "Command: " + fullPath
                        + Environment.NewLine + " Source: " + ex.Source
                        + Environment.NewLine + " Message: " + ex.Message
                        + Environment.NewLine + " Stack Trace: " + ex.StackTrace
                        + Environment.NewLine + " Inner Exception: " + ex.InnerException;
                return 101;
            }
        }



        public static int ExecuteCommandNoWait(string command, ref string msg, bool waitUntilEnd)
        {
            try
            {

                waitUntilEnd = false;


                int exitCode;
                ProcessStartInfo processInfo;

                processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
                //processInfo = new ProcessStartInfo(@"D:\WebSites\cps2\Batch\Interface\Cpsii.Task.WinForms.exe", "ImportIamsUser");


                //processInfo = new ProcessStartInfo(command);


                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // *** Redirect the output ***
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;

                process = Process.Start(processInfo);

                if (waitUntilEnd)
                {
                    process.WaitForExit();


                    // *** Read the streams ***
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    exitCode = process.ExitCode;

                    msg += Environment.NewLine + "output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output);
                    msg += Environment.NewLine + "error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error);
                    msg += Environment.NewLine + "ExitCode: " + exitCode.ToString();

                    process.Close();

                    if (!String.IsNullOrEmpty(error))
                        return -1;

                }
                else
                {
                    msg += " Scheduler is unable to determine the job execution results.";

                    return 0;

                }

                return 0;

            }
            catch (Exception ex)
            {
                msg += "Error encountered while triggering the ExecuteCommandEXE "
                        + Environment.NewLine + "Command: " + command
                        + Environment.NewLine + " Source: " + ex.Source
                        + Environment.NewLine + " Message: " + ex.Message
                        + Environment.NewLine + " Stack Trace: " + ex.StackTrace
                        + Environment.NewLine + " Inner Exception: " + ex.InnerException;
                return -3;
            }
        }

        public static int ExecuteCommandWaitTime(string command, ref string msg, bool waitUntilEnd)
        {
            try
            {

                int exitCode;
                ProcessStartInfo processInfo;

                processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
                //processInfo = new ProcessStartInfo(@"D:\WebSites\cps2\Batch\Interface\Cpsii.Task.WinForms.exe", "ImportIamsUser");

                //processInfo = new ProcessStartInfo(command);


                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // *** Redirect the output ***
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;

                process = Process.Start(processInfo);

                if (waitUntilEnd)
                {
                    process.WaitForExit(60000);


                    // *** Read the streams ***
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    exitCode = process.ExitCode;

                    msg += Environment.NewLine + "output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output);
                    msg += Environment.NewLine + "error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error);
                    msg += Environment.NewLine + "ExitCode: " + exitCode.ToString();

                    process.Close();

                    if (!String.IsNullOrEmpty(error))
                        return -1;

                }
                else
                {
                    msg += " Scheduler is unable to determine the job execution results.";

                    return 0;

                }

                return 0;

            }
            catch (Exception ex)
            {
                msg += "Error encountered while triggering the ExecuteCommandEXE "
                        + Environment.NewLine + "Command: " + command
                        + Environment.NewLine + " Source: " + ex.Source
                        + Environment.NewLine + " Message: " + ex.Message
                        + Environment.NewLine + " Stack Trace: " + ex.StackTrace
                        + Environment.NewLine + " Inner Exception: " + ex.InnerException;
                return -3;
            }
        }



        public static void Log(string functionName, string message, EventLogEntryType eventType, string logSrc, string log)
        {
            EventLog eventLog = new EventLog();

            if (!EventLog.SourceExists(logSrc))
            {
                EventLog.CreateEventSource(logSrc, log);
            }

            eventLog.Source = logSrc;
            eventLog.Log = log;

            eventLog.WriteEntry(message, eventType);

            if (eventType == EventLogEntryType.Error)
            {
                //TODO: log to exception db
                //ErrorLogDb errorLogDb = new ErrorLogDb();
                //errorLogDb.Insert(functionName, message, DateTime.Now);
            }
        }


     
        public static void SendEmail(string message, string heading)
        {
            message = message.Replace(Environment.NewLine, "<br />");

            string ccEmail = "";

            string senderName = "CONNECT Plan System II";
            string senderEmail = "cps2_admin@moe.gov.sg";
            string recipientEmail = Retrieve.GetRecipientEmail();





            message += "<br />Host: " + GetHostUrl();

            message += GetFixedFooter();

            string cr = Environment.NewLine;



            if (Retrieve.IsUAT().Trim().ToUpper() == "TRUE")
            {
                heading = heading + " (UAT Email)";
                message += cr + cr + "<br /><br />-----<br /><br />This is a UAT email from " + Util.GetHostUrl() + ". The original recipients are: " + recipientEmail;
                //message += cr + cr + "<br /><br />-----<br /><br />This is a UAT email. The original recipients are: " + recipientEmail;


                if (!string.IsNullOrEmpty(ccEmail))
                {
                    message = message + ", CC: " + ccEmail;
                }

                recipientEmail = Retrieve.GetUATRecipientEmail();
                ccEmail = "";
            }
          

            message = message.Replace(cr, "<br />");
            message = message.Replace("\n\r", "<br />");
            message = message.Replace("\r\n", "<br />");
            message = message.Replace("\n", "<br />");
            message = message.Replace("\r", "<br />");


            string replyToEmail = String.Empty;

            string subject = "[CPS2] Scheduler " + heading;

            EmailUtil emailUtil = new EmailUtil(senderEmail, senderName);

            emailUtil.SendEmail(recipientEmail, ccEmail, subject, message);
        }

        public static string GetHostUrl()
        {
          return System.Net.Dns.GetHostName();
        }

     

        public static string GetFixedFooter()
        {
            string s = "";
            string cr = Environment.NewLine;
            s += cr + "========================================";
            s += cr + "THIS IS A SYSTEM GENERATED MESSAGE";
            s += cr + "PLEASE DO NOT REPLY";
            s += cr + "========================================";

            return s;
        }


    }



}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Data;
using System.Data.SqlClient;

namespace CJ_Job_Scheduler.App_Code.Helper
{
    class Retrieve
    {
        public static int GetHeartbeatInterval()
        {
            return int.Parse(ConfigurationManager.AppSettings["HeartbeatInterval"].Trim());
        }

        public static string GetApplicationPath()
        {
            return ConfigurationManager.AppSettings["ApplicationPath"].Trim();
        }

        public static string GetRecipientEmail()
        {
            return ConfigurationManager.AppSettings["RecipientEmail"].Trim();
        }
        public static string GetDefaultCulture()
        {
            return ConfigurationManager.AppSettings["DefaultCulture"].Trim();
        }
        public static string GetScheduleFile()
        {
            return ConfigurationManager.AppSettings["ScheduleFile"].Trim();
        }
        public static string GetScheduleDetailFile()
        {
            return ConfigurationManager.AppSettings["ScheduleDetailFile"].Trim();
        }
        public static string GetUATRecipientEmail()
        {
            return ConfigurationManager.AppSettings["UATRecipientEmail"].Trim();
        }

        public static string IsUAT()
        {
            return ConfigurationManager.AppSettings["IsUAT"].Trim();
        }

     

    }

}

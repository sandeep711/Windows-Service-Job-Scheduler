using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Globalization;
using CJ_Job_Scheduler.App_Code.Helper;

namespace CJ_Job_Scheduler
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo(Retrieve.GetDefaultCulture());

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				//new Service1() 
                new CJ_Job_Scheduler_Service()
		};
            ServiceBase.Run(ServicesToRun);
        }
    }
}

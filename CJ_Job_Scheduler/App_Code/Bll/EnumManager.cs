using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CJ_Job_Scheduler.App_Code.Bll
{
    public enum FrequencyType
    {
        Daily,
        Monthly,
        Yearly
    }
    public enum ParameterNameEnum
    {
        SenderName,
        BatchJobMailingList,
        UatEmailList,
        RedirectEmailToUatEmailList,
        SystemEmail,
        EnableErrorNotification,
        ErrorNotificationMailingList,
        AuthenticationMode,
        SchedulerLatestHeartbeatDate
    }

    public enum RunMode
    { 
        Schedule,
        ForceStart,
        ForceStartNoWait,
        ForceStartWaitTime,
        Stop
    }




}

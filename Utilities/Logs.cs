using NLog;

namespace Utilities
{
    public class Logs
    {
        public static readonly Logger Log = LogManager.GetLogger("Console");
        public static readonly Logger ErrorLog = LogManager.GetLogger("ErrorLog");
       

        public static readonly Logger SummaryJSONLog = LogManager.GetLogger("SummaryJSONLog");
        public static readonly Logger phoneProjectListLog = LogManager.GetLogger("PhoneProjectListLog");
        public static readonly Logger phoneSolutionListLog = LogManager.GetLogger("PhoneSolutionListLog");

        public static readonly Logger TempLog = LogManager.GetLogger("TempLog");
        public static readonly Logger TempLog2 = LogManager.GetLogger("TempLog2");
        public static readonly Logger TempLog3 = LogManager.GetLogger("TempLog3");
        public static readonly Logger TempLog4 = LogManager.GetLogger("TempLog4");
        public static readonly Logger TempLog5 = LogManager.GetLogger("TempLog5");

        public static readonly Logger CallTraceLog = LogManager.GetLogger("CallTraceLog");

        public static readonly Logger SyncClassifierLog = LogManager.GetLogger("SyncClassifierLog");

        public static readonly Logger AsyncClassifierLog = LogManager.GetLogger("AsyncClassifierLog");
        public static readonly Logger AsyncClassifierOriginalLog = LogManager.GetLogger("AsyncClassifierOriginalLog");

        public static readonly Logger APMDiagnosisLog = LogManager.GetLogger("APMDiagnosisLog");

    }
}

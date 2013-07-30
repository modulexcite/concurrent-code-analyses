using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Utilities
{
    public class Logs
    {
        public static readonly Logger Log = LogManager.GetLogger("Console");

        public static readonly Logger SummaryJSONLog = LogManager.GetLogger("SummaryJSONLog");
        public static readonly Logger phoneProjectListLog = LogManager.GetLogger("PhoneProjectListLog");
        public static readonly Logger phoneSolutionListLog = LogManager.GetLogger("PhoneSolutionListLog");


        public static readonly Logger TempLog = LogManager.GetLogger("TempLog");

        public static readonly Logger CallTraceLog = LogManager.GetLogger("CallTraceLog");

        public static readonly Logger SyncClassifierLog = LogManager.GetLogger("SyncClassifierLog");

        public static readonly Logger AsyncClassifierLog = LogManager.GetLogger("AsyncClassifierLog");
        public static readonly Logger AsyncClassifierOriginalLog = LogManager.GetLogger("AsyncClassifierOriginalLog");

        public static readonly Logger APMDiagnosisLog = LogManager.GetLogger("APMDiagnosisLog");
        public static readonly Logger APMDiagnosisLog2 = LogManager.GetLogger("APMDiagnosisLog2");

    }
}

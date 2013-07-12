using System.IO;
using Utilities;

namespace Analysis
{
    public class ThreadTaskAnalysisResult : AnalysisResultBase
    {
        public int IsTpl;
        public int IsThreading;
        public int IsDataflow;
        public int NumberOfTaskInstances;
        public int NumberOfThreadInstances;

        //private readonly StreamWriter _appsFileWriter;

        public ThreadTaskAnalysisResult(string appName)
            : base(appName)
        {
        }

        public override void WriteSummaryLog()
        {
            //_appsFileWriter.Write(
            //                   _appName + "," + IsTpl + "," + IsThreading + "," + IsDataflow +
            //                   "," + NumberOfTaskInstances + "," + NumberOfThreadInstances + "," +
            //                   (NumberOfThreadInstances + NumberOfTaskInstances) + "\n");
        }
    }
}

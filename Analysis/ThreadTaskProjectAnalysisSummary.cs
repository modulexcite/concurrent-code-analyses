using Utilities;

namespace Analysis
{
    public class ThreadTaskProjectAnalysisSummary : ProjectAnalysisSummary
    {
        private const string AppsFile = @"C:\Users\Semih\Desktop\apps.txt";

        public int IsTpl;
        public int IsThreading;
        public int IsDataflow;
        public int NumberOfTaskInstances;
        public int NumberOfThreadInstances;

        public ThreadTaskProjectAnalysisSummary(string appName) : base(appName) { }

        public override void WriteResults()
        {
            Helper.WriteLogger(AppsFile,
                               AppName + "," + IsTpl + "," + IsThreading + "," + IsDataflow +
                               "," + NumberOfTaskInstances + "," + NumberOfThreadInstances + "," +
                               (NumberOfThreadInstances + NumberOfTaskInstances) + "\n");
        }
    }
}

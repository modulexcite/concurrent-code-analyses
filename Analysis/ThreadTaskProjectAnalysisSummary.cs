using System.IO;
using Utilities;

namespace Analysis
{
    public class ThreadTaskProjectAnalysisSummary : ProjectAnalysisSummary
    {
        public int IsTpl;
        public int IsThreading;
        public int IsDataflow;
        public int NumberOfTaskInstances;
        public int NumberOfThreadInstances;

        private readonly StreamWriter _appsFileWriter;

        public ThreadTaskProjectAnalysisSummary(string appName, StreamWriter appsFileWriter)
            : base(appName)
        {
            _appsFileWriter = appsFileWriter;
        }

        public override void WriteResults()
        {
            _appsFileWriter.Write(
                               AppName + "," + IsTpl + "," + IsThreading + "," + IsDataflow +
                               "," + NumberOfTaskInstances + "," + NumberOfThreadInstances + "," +
                               (NumberOfThreadInstances + NumberOfTaskInstances) + "\n");
        }
    }
}

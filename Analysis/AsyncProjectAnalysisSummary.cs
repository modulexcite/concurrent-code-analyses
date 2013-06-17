using System;
using System.IO;
using Utilities;

namespace Analysis
{
    public class AsyncProjectAnalysisSummary : ProjectAnalysisSummary
    {
        public int NumUIClasses;
        public int NumEventHandlerMethods;
        public int NumAsyncMethods;

        private readonly StreamWriter _appsFileWriter;
        public int[] NumPatternUsages;

        public AsyncProjectAnalysisSummary(string appName, StreamWriter appsFileWriter)
            : base(appName)
        {
            _appsFileWriter = appsFileWriter;
            NumPatternUsages = new int[11];
        }

        public override void WriteResults()
        {
            _appsFileWriter.Write(
                               AppName + "," +
                               NumTotalProjects + "," +
                               NumUnloadedProjects + "," +
                               NumUnanalyzedProjects + "," +
                               NumAzureProjects + "," +
                               NumPhoneProjects + "," +
                               NumPhone8Projects + "," +
                               NumNet4Projects + "," +
                               NumNet45Projects + "," +
                               NumOtherNetProjects + ",");

            foreach (var pattern in NumPatternUsages)
                _appsFileWriter.Write(pattern + ",");

            _appsFileWriter.Write(
                               NumAsyncMethods + ", " + NumEventHandlerMethods + "," + NumUIClasses +
                               "\r\n");
        }
    }
}

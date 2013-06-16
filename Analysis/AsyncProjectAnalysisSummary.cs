using System;
using Utilities;

namespace Analysis
{
    public class AsyncProjectAnalysisSummary : ProjectAnalysisSummary
    {
        private const string AppsFile = @"C:\Users\david\Desktop\UIStatistics.txt";

        public int NumUIClasses;
        public int NumEventHandlerMethods;
        public int NumAsyncMethods;

        public int[] NumPatternUsages;

        public AsyncProjectAnalysisSummary(string appName)
            : base(appName)
        {
            NumPatternUsages = new int[11];
        }

        public override void WriteResults()
        {
            Helper.WriteLogger(AppsFile,
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
                Helper.WriteLogger(AppsFile, pattern + ",");

            Helper.WriteLogger(AppsFile,
                               NumAsyncMethods + ", " + NumEventHandlerMethods + "," + NumUIClasses +
                               "\r\n");
        }
    }
}

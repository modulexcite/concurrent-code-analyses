using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analysis;
using Microsoft.Build.Evaluation;

namespace Collector
{
    internal class Collector
    {
        public const string LogFile = @"C:\Users\david\Desktop\collector.log";

        private const string InterestingCallsFile = @"C:\Users\david\Desktop\callsFromEventHandlers.txt";
        private const string AppsFile = @"C:\Users\david\Desktop\UIStatistics.txt";

        private const string TempFile = @"C:\Users\david\Desktop\temp.txt";

        private readonly List<string> _analyzedProjects;
        private readonly IEnumerable<string> _subdirsToAnalyze;

        public Collector(String topDir, int batchSize)
        {
            _analyzedProjects = File.Exists(LogFile)
                ? Collector.AnalyzedProjectsFromLogFileContents()
                : new List<string>();

            _subdirsToAnalyze = Directory.GetDirectories(topDir)
                                         .Where(IsNotYetAnalyzed)
                                         .OrderBy(s => s)
                /*.Take(batchSize)*/;
        }

        private bool IsNotYetAnalyzed(string subdir)
        {
            return !_analyzedProjects.Any(s => subdir.Split('\\').Last().Equals(s));
        }

        public void Run()
        {
            var interestingCallsWriter = new StreamWriter(InterestingCallsFile, true);
            var appsFileWriter = new StreamWriter(AppsFile, true);
            var logFileWriter = new StreamWriter(LogFile, true);
            var callTraceWriter = new StreamWriter(TempFile, true);

            foreach (var subdir in _subdirsToAnalyze)
            {
                var appName = subdir.Split('\\').Last();
                var appSummary = new AsyncProjectAnalysisSummary(appName, appsFileWriter);

                var app = new AsyncProjectAnalysis(subdir, appSummary, interestingCallsWriter, callTraceWriter);

                Console.WriteLine(appName);

                app.Analyze();

                appSummary.WriteResults(logFileWriter);

                ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            }

            interestingCallsWriter.Dispose();
            appsFileWriter.Dispose();
            logFileWriter.Dispose();
            callTraceWriter.Dispose();
        }

        private static List<string> AnalyzedProjectsFromLogFileContents()
        {
            return File.ReadAllLines(LogFile)
                       .Select(a => a.Split(',')[0])
                       .ToList();
        }
    }
}

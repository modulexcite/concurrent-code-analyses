﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analysis;
using Microsoft.Build.Evaluation;
using NLog;

namespace Collector
{
    internal class Collector
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

            var index = 0;
            foreach (var subdir in _subdirsToAnalyze)
            {
                var appName = subdir.Split('\\').Last();
                var appSummary = new AsyncProjectAnalysisSummary(appName, appsFileWriter);

                var app = new AsyncProjectAnalysis(subdir, appSummary, interestingCallsWriter, callTraceWriter);

                Log.Info(@"{0}: {1}", index, appName);

                app.Analyze();

                appSummary.WriteResults(logFileWriter);

                ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

                interestingCallsWriter.Flush();
                appsFileWriter.Flush();
                logFileWriter.Flush();
                callTraceWriter.Flush();

                index++;
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

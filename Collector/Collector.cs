using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analysis;
using Microsoft.Build.Evaluation;
using NLog;
using System.Configuration;

namespace Collector
{
    internal class Collector
    {
        private static readonly Logger Log = LogManager.GetLogger("Console");
        private static string SummaryLogFileName = ConfigurationManager.AppSettings["SummaryLogFile"];

        private readonly List<string> _analyzedProjects;
        private readonly IEnumerable<string> _subdirsToAnalyze;

        public Collector(String topDir, int batchSize)
        {
            _analyzedProjects = File.Exists(SummaryLogFileName)
                ? Collector.AnalyzedProjectsFromLogFileContents()
                : new List<string>();

            _subdirsToAnalyze = Directory.GetDirectories(topDir)
                                         .Where(IsNotYetAnalyzed)
                                         .OrderBy(s => s)
                                         .Take(batchSize);
        }

        private bool IsNotYetAnalyzed(string subdir)
        {
            return !_analyzedProjects.Any(s => subdir.Split('\\').Last().Equals(s));
        }

        public void Run()
        {
            var index = 1;

            foreach (var subdir in _subdirsToAnalyze)
            {
                var appName = subdir.Split('\\').Last();
                
                var app = new AsyncAnalysis(subdir, appName);

                Log.Info(@"{0}: {1}", index, appName);

                app.Analyze();

                ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
                
                index++;
            }
        }

        private static List<string> AnalyzedProjectsFromLogFileContents()
        {
            return File.ReadAllLines(SummaryLogFileName)
                       .Select(a => a.Split(',')[0])
                       .ToList();
        }
    }
}

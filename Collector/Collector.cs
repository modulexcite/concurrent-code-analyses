using Analysis;
using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Collector
{
    internal class Collector
    {
        private static readonly Logger Log = LogManager.GetLogger("Console");


        private static string SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];

        private readonly List<string> _analyzedProjects;
        private readonly IEnumerable<string> _subdirsToAnalyze;

        public Collector(String topDir, int batchSize)
        {
            _analyzedProjects = File.Exists(SummaryJSONLogPath)
                ? Collector.AnalyzedAppsFromJSONLog()
                : new List<string>();

            _subdirsToAnalyze = Directory.GetDirectories(topDir)
                                         .Where(IsNotYetAnalyzed)
                                         .OrderBy(s => s)
                                         .Take(batchSize);
        }

        private static List<string> AnalyzedAppsFromJSONLog()
        {
            return File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json).AppName).ToList();
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


    }
}
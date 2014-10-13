using Analysis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Utilities;

namespace Collector
{
    internal class Collector
    {
        private static string SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];

        private readonly IEnumerable<string> _appsToAnalyze;

        public Collector(String[] apps, int batchSize)
        {
            var analyzedApps = File.Exists(SummaryJSONLogPath)
                ? Collector.AnalyzedAppsFromJSONLog()
                : new List<string>();

            _appsToAnalyze = apps.Where(s=> IsNotYetAnalyzed(s, analyzedApps)).OrderBy(s => s).Take(batchSize);
        }

        private static List<string> AnalyzedAppsFromJSONLog()
        {
            return File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<TaskifierAnalysisResult>(json).SolutionPath).ToList();
        }

        private static bool IsNotYetAnalyzed(string name, List<string> analyzedApps)
        {
            return !analyzedApps.Contains(name);
        }

        public void Run()
        {
            var index = 1;
            foreach (var solutionPath in _appsToAnalyze)
            {
                var appName = solutionPath.Split('\\').Last().Split('.').First();

                //var app = new AsyncAnalysis(subdir, appName);
                var app = new TaskifierAnalysis(solutionPath, appName);

                Logs.Console.Info(@"{0}: {1}", index, solutionPath);
                app.Analyze();
                index++;
            }
        }
    }
}
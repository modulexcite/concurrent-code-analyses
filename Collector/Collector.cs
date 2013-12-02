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

        private readonly List<string> _analyzedApps;
        private readonly IEnumerable<string> _appsToAnalyze;

        public Collector(String[] apps, int batchSize)
        {
            _analyzedApps = File.Exists(SummaryJSONLogPath)
                ? Collector.AnalyzedAppsFromJSONLog()
                : new List<string>();

            _appsToAnalyze = apps.Where(IsNotYetAnalyzed).OrderBy(s => s).Take(batchSize);
        }

        private static List<string> AnalyzedAppsFromJSONLog()
        {
            //todo: generic the type of deserializeobject
            return File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<TaskifierAnalysisResult>(json).AppName).ToList();
        }

        private bool IsNotYetAnalyzed(string subdir)
        {
            return !_analyzedApps.Any(s => subdir.Split('\\').Last().Equals(s));
        }

        public void Run()
        {
            var index = 1;

            foreach (var subdir in _appsToAnalyze)
            {
                var appName = subdir.Split('\\').Last();

                //var app = new AsyncAnalysis(subdir, appName);
                var app = new TaskifierAnalysis(subdir, appName);

                Logs.Log.Info(@"{0}: {1}", index, appName);

                app.Analyze();

                index++;
            }
        }
    }
}
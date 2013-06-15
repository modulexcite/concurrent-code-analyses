using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analysis;
using Utilities;

namespace Collector
{
    internal class Collector
    {
        public const string LogFile = @"C:\Users\david\Desktop\collector.log";

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
                                         .Take(batchSize);
        }

        private bool IsNotYetAnalyzed(string subdir)
        {
            return !_analyzedProjects.Any(s => subdir.Split('\\').Last().Equals(s));
        }

        public void Run()
        {
            foreach (var subdir in _subdirsToAnalyze)
            {
                var appName = subdir.Split('\\').Last();
                var app = new AsyncAnalysis(appName, subdir);

                Console.WriteLine(appName);

                app.LoadSolutions();
                app.Analyze();

                app.WriteResults(LogFile);
            }
        }

        private static List<string> AnalyzedProjectsFromLogFileContents()
        {
            return File.ReadAllLines(LogFile)
                       .Select(a => a.Split(',')[0])
                       .ToList();
        }
    }
}
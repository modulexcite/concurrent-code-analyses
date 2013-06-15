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
        private const string LogFile = @"C:\Users\david\Desktop\collector.log";

        private readonly List<string> _analyzedProjects;
        private readonly IEnumerable<string> _subdirsToAnalyze;

        public Collector(String topDir, int batchSize)
        {
            if (File.Exists(LogFile))
            {
                _analyzedProjects = Collector.AnalyzedProjectsFromLogFileContents();
            }
            else
            {
                _analyzedProjects = new List<string>();
            }

            _subdirsToAnalyze = Directory.GetDirectories(topDir)
                                         .Where(subdir => !_analyzedProjects.Any(s => subdir.Split('\\').Last().Equals(s)))
                                         .OrderBy(s => s)
                                         .Take(batchSize);
        }

        public void Run()
        {
            foreach (var subdir in _subdirsToAnalyze)
            {
                String appName = subdir.Split('\\').Last();
                AnalysisBase app = new AsyncAnalysis(appName, subdir);

                Console.WriteLine(appName);

                app.LoadSolutions();
                app.Analyze();

                var logText = app.appName + "," + app.numTotalProjects + "," + app.numUnloadedProjects + "," + app.numUnanalyzedProjects + "\r\n";
                Helper.WriteLogger(LogFile, logText);
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
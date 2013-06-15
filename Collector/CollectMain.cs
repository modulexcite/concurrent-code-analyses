﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Analysis;
using Utilities;

namespace Collector
{
    class CollectMain
    {
        static void Main(string[] args)
        {
            Start(args);
        }

        static void Start(string[] args)
        {
            if (args.Length > 0)
            {
                var batchSize = int.Parse(args[0]);
                var topDir = args[1];

                var collector = new Collector(topDir, batchSize);
                collector.Run();
            }
            else
            {
                var collector = new Collector(@"C:\Users\david\Downloads\C# Projects\Candidates", 1000);
                collector.Run();
            }
        }

        public class Collector
        {
            private const string LogFile = @"C:\Users\david\Desktop\collector.log";

            private readonly List<String> _analyzedProjects;
            private readonly IEnumerable<String> _subdirsToAnalyze;

            public Collector(String topDir, int batchSize)
            {
                if (File.Exists(LogFile))
                {
                    _analyzedProjects = Collector.OldAnalyzedProjectsLogFileContents();
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

            private static List<string> OldAnalyzedProjectsLogFileContents()
            {
                return File.ReadAllLines(LogFile)
                             .Select(a => a.Split(',')[0])
                             .ToList();
            }
        }
    }
}

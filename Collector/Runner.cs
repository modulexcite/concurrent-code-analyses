using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Analysis;


namespace AnalysisRunner
{
    class Runner
    {
        private static string CodeCorpusPath = ConfigurationManager.AppSettings["CodeCorpus"];
        private static string SubsetApps = ConfigurationManager.AppSettings["SubsetApps"];

        public static AnalysisType[] AnalysisTypes = { AnalysisType.AsyncAwaitUsage };
        private static void Main(string[] args)
        {
            StartAnalysis();
            Console.WriteLine("**************FINISHED***************");
            Console.ReadKey();
        }

        private static void StartAnalysis()
        {
            string[] appsToAnalyze;
            if (bool.Parse(ConfigurationManager.AppSettings["OnlyAnalyzeSubsetApps"]))
                appsToAnalyze = File.ReadAllLines(SubsetApps).Select(appName => CodeCorpusPath + appName).ToArray<string>();
            else
                appsToAnalyze = Directory.GetDirectories(CodeCorpusPath).ToArray<string>();


            using (var context = new ResultDatabaseContext())
            {
                var result = new Result()
                {
                    Date = DateTime.Now,
                    Apps = new List<App>(),
                    AnalysisTypes = AnalysisTypes
                };

                context.Results.Add(result);

                foreach (var appPath in appsToAnalyze)
                {
                    // TODO: Extract the Name from appPath
                    var app = new App()
                    {
                        Name = appPath,
                        AppPath = appPath,
                        Projects = new List<Project>()
                    };

                    app.PerformAnalysis();

                    result.Apps.Add(app);
                    context.SaveChanges();
                }

            }
        }
    }
}
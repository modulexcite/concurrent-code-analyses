using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Collector
{
    internal class CollectMain
    {
        private static string AppsPath = ConfigurationManager.AppSettings["AppsPath"];
        private static string AsyncAwaitApps = ConfigurationManager.AppSettings["AsyncAwaitApps"];

        private static void Main(string[] args)
        {

            string[] appsToAnalyze;
            if (bool.Parse(ConfigurationManager.AppSettings["OnlyAnalyzeAsyncAwaitApps"]))
                appsToAnalyze = File.ReadAllLines(AsyncAwaitApps).Select(appName => AppsPath+appName).ToArray<string>();
            else
                appsToAnalyze = Directory.GetDirectories(AppsPath).ToArray<string>();

            var collector = new Collector(appsToAnalyze, 1000);
            collector.Run();
            
            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadKey();
        }
    }
}
using Analysis;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Utilities;

namespace Collector
{
    internal class CollectMain
    {
        private static string AppsPath = ConfigurationManager.AppSettings["AppsPath"];
        private static string SubsetApps = ConfigurationManager.AppSettings["SubsetApps"];

        private static void Main(string[] args)
        {

            StartAnalyze();
            //Results();
            //ExtractToCsvForAPM();
        }


        private static void StartAnalyze()
        {

            string[] appsToAnalyze;
            if (bool.Parse(ConfigurationManager.AppSettings["OnlyAnalyzeSubsetApps"]))
                appsToAnalyze = File.ReadAllLines(SubsetApps).Select(appName => AppsPath + appName).ToArray<string>();
            else
                appsToAnalyze = Directory.GetDirectories(AppsPath).ToArray<string>();

            var collector = new Collector(appsToAnalyze, 1000);
            collector.Run();

            

            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadLine();
        }


        private static void Results()
        {
            var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
            var results= File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json)).ToList();

            

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\subsetApps.txt"))
            {
                foreach (var result in results)
                {
                    if(result.asyncAwaitResults.NumAsyncTaskMethods+ result.asyncAwaitResults.NumAsyncVoidEventHandlerMethods+result.asyncAwaitResults.NumAsyncVoidNonEventHandlerMethods>0)
                        file.WriteLine("{0}", result.AppName);
                }
            }
            

            //Console.WriteLine(results.Where(a => a.generalResults.NumTotalSLOC > 1000 ).Select(a=> a.asyncUsageResults.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.APM]).Sum());


            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadKey();
        
        }

        private static void ExtractToCsv()
        {
            var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
            var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json)).ToList();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\summary.csv"))
            {
                file.WriteLine("name,total,unanalyzed,wp7,wp8,slocwp7,slocwp8,sloc,apmwp7,apmwp8,apm,eap,tap,async/await");
                foreach (var result in results)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}",
                        result.AppName,
                        result.generalResults.NumTotalProjects,
                        result.generalResults.NumUnanalyzedProjects,
                        result.generalResults.NumPhone7Projects,
                        result.generalResults.NumPhone8Projects,
                        result.generalResults.SLOCWP7,
                        result.generalResults.SLOCWP8,
                        result.generalResults.NumTotalSLOC,
                        result.asyncUsageResults.APMWP7,
                        result.asyncUsageResults.APMWP8,
                        result.asyncUsageResults.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.APM],
                        result.asyncUsageResults.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.EAP],
                        result.asyncUsageResults.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.TAP],
                        result.asyncAwaitResults.NumAsyncTaskMethods + result.asyncAwaitResults.NumAsyncVoidEventHandlerMethods + result.asyncAwaitResults.NumAsyncVoidNonEventHandlerMethods);

                }

            }
        
        }




        private static void ExtractToCsvForAPM()
        {
            var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
            var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json)).ToList();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\summaryAPMDiagnosis.csv"))
            {
                file.WriteLine("name,total,unanalyzed,wp7,wp8,slocwp7,slocwp8,sloc,apmbegin,apmend,apmbeginfollowed,apmendnested,apmendtrycatched");
                foreach (var result in results)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
                        result.AppName,
                        result.generalResults.NumTotalProjects,
                        result.generalResults.NumUnanalyzedProjects,
                        result.generalResults.NumPhone7Projects,
                        result.generalResults.NumPhone8Projects,
                        result.generalResults.SLOCWP7,
                        result.generalResults.SLOCWP8,
                        result.generalResults.NumTotalSLOC,
                        result.apmDiagnosisResults.NumAPMBeginMethods,
                        result.apmDiagnosisResults.NumAPMEndMethods,
                        result.apmDiagnosisResults.NumAPMBeginFollowed,
                        result.apmDiagnosisResults.NumAPMEndNestedMethods,
                        result.apmDiagnosisResults.NumAPMEndTryCatchedMethods);
                }

            }

        }
    }
}
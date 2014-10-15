using Analysis;
using ConsultingAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private static string SolutionFiles = ConfigurationManager.AppSettings["SolutionFiles"];


        private static void Main(string[] args)
        {
            PerformAnalysis();
            Console.WriteLine("**************FINISHED***************");
            Console.ReadKey();
        }

        private static void PerformAnalysis()
        {
            //Test();
            FindSolutionFiles(AppsPath);
            StartBatchAnalysis();
            ConsultingAnalysisResult.ExtractToCsv();
            //ExtractToCsvForBasicResult();


            //Test();
        }

        private static void Test()
        {
            var app = new ConsultingAnalysis.ConsultingAnalysis(@"C:\build\PHMC\UnitTests\Libraries\Messaging\WebMD.UnitTests.Messaging.csproj", "temp");
            app.Analyze();
        }

        private static void StartBatchAnalysis()
        {
            string[] appsToAnalyze;
            if (bool.Parse(ConfigurationManager.AppSettings["AnalyzeOSS"]))
            {
                if (bool.Parse(ConfigurationManager.AppSettings["OnlyAnalyzeSubsetApps"]))
                    appsToAnalyze = File.ReadAllLines(SubsetApps).Select(appName => AppsPath + appName).ToArray<string>();
                else
                    appsToAnalyze = Directory.GetDirectories(AppsPath).ToArray<string>();
            }
            else
            {
                appsToAnalyze = File.ReadAllLines(SolutionFiles);     
            }
            var collector = new Collector(appsToAnalyze, 100000);
            collector.Run();
        }

        private static void FindSolutionFiles(string mainDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(mainDir))
                {
                    foreach (string f in Directory.GetFiles(d, "*.csproj"))
                    {
                        Logs.SolutionFiles.Info(f);
                    }
                    FindSolutionFiles(d);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private static void FilterSummaryForSubSetAnalysis()
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
        }

        private static void ExtractToCsvForBasicResult()
        {
            var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
            var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<ConsultingAnalysisResult>(json)).ToList();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\sokur\Desktop\basicSummary.csv"))
            {
                file.WriteLine("name,total,unanalyzed,.net45,.net4,other.net,phone7,phone8,sloc");
                foreach (var result in results)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                            result.AppName,
                            result.generalResults.NumTotalProjects,
                            result.generalResults.NumUnanalyzedProjects,
                            result.generalResults.NumNet45Projects,
                            result.generalResults.NumNet4Projects,
                            result.generalResults.NumOtherNetProjects,
                            result.generalResults.NumPhone7Projects,
                            result.generalResults.NumPhone8Projects,
                            result.generalResults.NumTotalSLOC
                            );
                    
                }

            }

        }

        //private static void ExtractToCsvForAsyncAnalysisResult()
        //{
        //    var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
        //    var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json)).ToList();
        //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\summary.csv"))
        //    {
        //        file.WriteLine("name,total,unanalyzed,wp7,wp8,slocwp7,slocwp8,sloc,apmwp7,apm8,eap7,eap8,tap7,tap8,aa7,aa8,thread7,thread8,asyncdelegate7,asyncdelegate8,backgroundworker7,backgroundworker8,threadpool7,threadpool8,task7,task8");
        //        foreach (var result in results)
        //        {
        //            if (result.generalResults.NumTotalSLOC > 499)
        //            {
        //                file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25}",
        //                    result.AppName,
        //                    result.generalResults.NumTotalProjects,
        //                    result.generalResults.NumUnanalyzedProjects,
        //                    result.generalResults.NumPhone7Projects,
        //                    result.generalResults.NumPhone8Projects,
        //                    result.generalResults.SLOCWP7,
        //                    result.generalResults.SLOCWP8,
        //                    result.generalResults.NumTotalSLOC,
        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.APM],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.APM],

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.EAP],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.EAP],

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.TAP],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.TAP],

        //                    result.asyncUsageResults_WP7.NumAsyncAwaitMethods,
        //                    result.asyncUsageResults_WP8.NumAsyncAwaitMethods,

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.Thread],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.Thread],

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.AsyncDelegate],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.AsyncDelegate],

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.BackgroundWorker],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.BackgroundWorker],

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.Threadpool],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.Threadpool],

        //                    result.asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.Task],
        //                    result.asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)Enums.AsyncDetected.Task]
        //                    );
        //            }
        //        }

        //    }
        
        //}


        //private static void ExtractToCsvAsyncAwait()
        //{
        //    var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
        //    var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json)).ToList();
        //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\summary.csv"))
        //    {
        //        file.WriteLine("name,total,unanalyzed,wp7,wp8,slocwp7,slocwp8,sloc,asyncawait_wp7,asyncawait_wp8,asyncvoideventhandler,asyncvoidnoneventhandler,configureawait");
        //        foreach (var result in results)
        //        {
        //            if (result.generalResults.NumTotalSLOC > 499)
        //            {
        //                file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
        //                    result.AppName,
        //                    result.generalResults.NumTotalProjects,
        //                    result.generalResults.NumUnanalyzedProjects,
        //                    result.generalResults.NumPhone7Projects,
        //                    result.generalResults.NumPhone8Projects,
        //                    result.generalResults.SLOCWP7,
        //                    result.generalResults.SLOCWP8,
        //                    result.generalResults.NumTotalSLOC,
        //                    result.asyncAwaitResults.NumAsyncAwaitMethods_WP7,
        //                    result.asyncAwaitResults.NumAsyncAwaitMethods_WP8,
        //                    result.asyncAwaitResults.NumAsyncVoidEventHandlerMethods,
        //                    result.asyncAwaitResults.NumAsyncVoidNonEventHandlerMethods,
        //                    result.asyncAwaitResults.NumAsyncMethodsHavingConfigureAwait);
        //            }
        //        }

        //    }

        //}

        //private static void ExtractToCsvForAPM()
        //{
        //    var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
        //    var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<AsyncAnalysisResult>(json)).ToList();
        //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\summaryAPMDiagnosis.csv"))
        //    {
        //        file.WriteLine("name,total,unanalyzed,wp7,wp8,slocwp7,slocwp8,sloc,apmbegin,apmend,apmbeginfollowed,apmendnested,apmendtrycatched");
        //        foreach (var result in results)
        //        {
        //            file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
        //                result.AppName,
        //                result.generalResults.NumTotalProjects,
        //                result.generalResults.NumUnanalyzedProjects,
        //                result.generalResults.NumPhone7Projects,
        //                result.generalResults.NumPhone8Projects,
        //                result.generalResults.SLOCWP7,
        //                result.generalResults.SLOCWP8,
        //                result.generalResults.NumTotalSLOC,
        //                result.apmDiagnosisResults.NumAPMBeginMethods,
        //                result.apmDiagnosisResults.NumAPMEndMethods,
        //                result.apmDiagnosisResults.NumAPMBeginFollowed,
        //                result.apmDiagnosisResults.NumAPMEndNestedMethods,
        //                result.apmDiagnosisResults.NumAPMEndTryCatchedMethods);
        //        }

        //    }
        //}
    }
}
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using NLog;
using System.Configuration;
using Utilities;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System;
using System.Collections.Generic;

namespace Analysis
{
    public class ConsultingAnalysisResult : AnalysisResult
    {
        public class CPUAsyncResults
        {
            public int NumThreadUsage;
            public int NumThreadPoolUsage;
            public int NumBackgWorkerUsage;
            public int NumAsyncDelegateUsage;
            public int NumTaskUsage;
            public int NumParallelForUsage;
            public int NumParallelInvokeUsage;
            public int NumParallelForEachUsage;
        }

        public class IOAsyncResults
        {
            public int NumAPMUsage;
            public int NumEAPUsage;
            public int NumTAPUsage;
        }

        public class AsyncAwaitResults
        {
            public int NumAsyncMethods;
            public int NumAsyncTaskMethods;
            public int NumFireForget;
            public int NumUnnecessaryAsync;
            public int NumUnnecessaryContext;
            public int NumLongRunning;
        }

        public static Dictionary<string, int> libraryUsage = new Dictionary<string, int>();

        public CPUAsyncResults cpuAsyncResults { get; set; }
        public IOAsyncResults ioAsyncResults { get; set; }
        public AsyncAwaitResults asyncAwaitResults { get; set; }

        public ConsultingAnalysisResult(string solutionPath, string appName)
            : base(solutionPath, appName)
        {
            cpuAsyncResults = new CPUAsyncResults();
            ioAsyncResults = new IOAsyncResults();
            asyncAwaitResults = new AsyncAwaitResults();
        }

        public bool ShouldSerializelibraryUsage()
        {
            return false;
        }

        public bool ShouldSerializecpuAsyncResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsCPUAsyncDetectionEnabled"]);
        }

        public bool ShouldSerializeioAsyncResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsAsyncAwaitDetectionEnabled"]);
        }

        public bool ShouldSerializeasyncAwaitResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsAsyncAwaitDetectionEnabled"]);
        }

        public override void WriteSummaryLog()
        {
            Logs.SummaryJSONLog.Info(@"{0}", JsonConvert.SerializeObject(this, Formatting.None));
        }

        internal void StoreDetectedCPUAsyncUsage(Enums.AsyncDetected type)
        {
            if (Enums.AsyncDetected.None != type)
            {
                switch (type)
                {
                    case Enums.AsyncDetected.Thread:
                        cpuAsyncResults.NumThreadUsage++;
                        break;
                    case Enums.AsyncDetected.Threadpool:
                        cpuAsyncResults.NumThreadPoolUsage++;
                        break;
                    case Enums.AsyncDetected.AsyncDelegate:
                        cpuAsyncResults.NumAsyncDelegateUsage++;
                        break;
                    case Enums.AsyncDetected.BackgroundWorker:
                        cpuAsyncResults.NumBackgWorkerUsage++;
                        break;
                    case Enums.AsyncDetected.Task:
                        cpuAsyncResults.NumTaskUsage++;
                        break;
                    case Enums.AsyncDetected.ParallelFor:
                        cpuAsyncResults.NumParallelForUsage++;
                        break;
                    case Enums.AsyncDetected.ParallelInvoke:
                        cpuAsyncResults.NumParallelInvokeUsage++;
                        break;
                    case Enums.AsyncDetected.ParallelForEach:
                        cpuAsyncResults.NumParallelForEachUsage++;
                        break;
                }
            }

        }

        internal void StoreDetectedIOAsyncUsage(Enums.AsyncDetected type)
        {
            if (Enums.AsyncDetected.None != type)
            {
                switch (type)
                {
                    case Enums.AsyncDetected.APM:
                        ioAsyncResults.NumAPMUsage++;
                        break;
                    case Enums.AsyncDetected.EAP:
                        ioAsyncResults.NumEAPUsage++;
                        break;
                    case Enums.AsyncDetected.TAP:
                        ioAsyncResults.NumTAPUsage++;
                        break;
                }
            }
        }

        internal void StoreDetectedAsyncMisuse(int type, Microsoft.CodeAnalysis.Document Document, MethodDeclarationSyntax node)
        {
            string name="";
            switch(type)
            {
                case 1:
                    name = "fireforget";
                    asyncAwaitResults.NumFireForget++;
                    break;
                case 2:
                    name = "unnecessaryasync";
                    asyncAwaitResults.NumUnnecessaryAsync++;
                    break;
                case 3:
                    name = "longrunning";
                    asyncAwaitResults.NumLongRunning++;
                    break;
                case 4:
                    name = "unnecessarycontext";
                    asyncAwaitResults.NumUnnecessaryContext++;
                    break;
            }
            Logs.AsyncMisuse.Info(@"{0} - {1}{2}**********************************************", Document.FilePath, name, node.ToLog());
        }

        public static void ExtractToCsv()
        {
            int i = 0;
            var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
            var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<ConsultingAnalysisResult>(json)).ToList();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\sokur\Desktop\consultingAnalysisResult.csv"))
            {
                file.WriteLine("name,total,unanalyzed,.net45,.net4,other.net,sloc, thread,threadpool,backworker,asyncdelegate,task,parallelfor, parallelforeach, parallelinvoke, apm, eap, tap, async, asyncTask, fireforget, unnecessaryAsync, unnecessaryContext, longrunning");
                foreach (var result in results)
                {
                    file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23}",
                            result.AppName,
                            result.generalResults.NumTotalProjects,
                            result.generalResults.NumUnanalyzedProjects,
                            result.generalResults.NumNet45Projects,
                            result.generalResults.NumNet4Projects,
                            result.generalResults.NumOtherNetProjects,
                            result.generalResults.NumTotalSLOC,
                            result.cpuAsyncResults.NumThreadUsage,
                            result.cpuAsyncResults.NumThreadPoolUsage,
                            result.cpuAsyncResults.NumBackgWorkerUsage,
                            result.cpuAsyncResults.NumAsyncDelegateUsage,
                            result.cpuAsyncResults.NumTaskUsage,
                            result.cpuAsyncResults.NumParallelForUsage,
                            result.cpuAsyncResults.NumParallelForEachUsage,
                            result.cpuAsyncResults.NumParallelInvokeUsage,
                            result.ioAsyncResults.NumAPMUsage,
                            result.ioAsyncResults.NumEAPUsage,
                            result.ioAsyncResults.NumTAPUsage,
                            result.asyncAwaitResults.NumAsyncMethods,
                            result.asyncAwaitResults.NumAsyncTaskMethods,
                            result.asyncAwaitResults.NumFireForget,
                            result.asyncAwaitResults.NumUnnecessaryAsync,
                            result.asyncAwaitResults.NumUnnecessaryContext,
                            result.asyncAwaitResults.NumLongRunning);
                }
            }

            //var keys = libraryUsage.Keys.ToList();
            //keys.Sort();
            //foreach (var key in keys)
            //{
            //    Logs.TempLog.Info("{0},{1}", key, libraryUsage[key]);
            //}
        }
    }
}
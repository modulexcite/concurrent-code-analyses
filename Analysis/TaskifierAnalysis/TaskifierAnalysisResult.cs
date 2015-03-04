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
namespace Analysis
{
    public class TaskifierAnalysisResult : AnalysisResult
    {
        public class ThreadingNamespaceResults
        {
            public int NumThreadClassUsage;
            public int NumThreadpoolClassUsage;
            public int NumOtherClassUsage;
        }

        public class TasksNamespaceResults
        {
            public int NumParallelClassUsage;
            public int NumTaskClassUsage;
            public int NumOtherClassUsage;
        }


        public class GeneralTaskifierResults
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

        public ThreadingNamespaceResults threadingNamespaceResults { get; set; }
        public TasksNamespaceResults tasksNamespaceResults { get; set; }
        public GeneralTaskifierResults generalTaskifierResults { get; set; }

        public TaskifierAnalysisResult(string solutionPath, string appName)
            : base(solutionPath, appName)
        {
            threadingNamespaceResults = new ThreadingNamespaceResults();
            tasksNamespaceResults = new TasksNamespaceResults();
            generalTaskifierResults = new GeneralTaskifierResults();
        }


        public bool ShouldSerializethreadingNamespaceResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsThreadUsageDetectionEnabled"]);
        }


        public bool ShouldSerializetasksNamespaceResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsTasksUsageDetectionEnabled"]);
        }

        public bool ShouldSerializegeneralTaskifierResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsGeneralTaskifierDetectionEnabled"]);
        }

        public override void WriteSummaryLog()
        {
            Logs.SummaryJSONLog.Info(@"{0}", JsonConvert.SerializeObject(this, Formatting.None));
        }

        internal void StoreDetectedThreadingNamespaceUsage(Enums.ThreadingNamespaceDetected type)
        {
            if (Enums.ThreadingNamespaceDetected.None != type)
            {
                switch (type)
                { 
                    case Enums.ThreadingNamespaceDetected.ThreadClass:
                        threadingNamespaceResults.NumThreadClassUsage++;
                        break;
                    case Enums.ThreadingNamespaceDetected.ThreadpoolClass:
                        threadingNamespaceResults.NumThreadpoolClassUsage++;
                        break;
                    case Enums.ThreadingNamespaceDetected.OtherClass:
                        threadingNamespaceResults.NumOtherClassUsage++;
                        break;
                }
            }
        }

        internal void WriteDetectedThreadingNamespaceUsage(Enums.ThreadingNamespaceDetected type, string documentPath, ISymbol symbol, SyntaxNode node)
        {
            if (Enums.ThreadingNamespaceDetected.None != type)
            {


                Logger usagelog=null;
                Logger typelog=null;

                switch (type)
                {
                    case Enums.ThreadingNamespaceDetected.ThreadClass:
                        usagelog = Logs.TempLog;
                        typelog = Logs.TempLog2;
                        break;
                    case Enums.ThreadingNamespaceDetected.ThreadpoolClass:
                        usagelog = Logs.TempLog3;
                        typelog = Logs.TempLog4;
                        break;
                    case Enums.ThreadingNamespaceDetected.OtherClass:
                        usagelog = Logs.TempLog5;
                        typelog = Logs.TempLog6;
                        break;
                }

                SyntaxNode block=null;
                var temp = node.Ancestors().OfType<BlockSyntax>();
                if (temp.Any())
                    block = temp.First();
                else
                    block = node.Ancestors().ElementAt(3);

                typelog.Info("{0};{1}", symbol.ContainingType, symbol.ToString());
                usagelog.Info("{0} {1}\r\n{2}\r\n--------------------------", symbol, documentPath,  block);


                //// Let's get rid of all specific information!
                //if (!symbol.ReturnsVoid)
                //    returntype = symbol.ReturnType.OriginalDefinition.ToString();

                //typelog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", AppName, documentPath, type.ToString(), returntype, symbol.OriginalDefinition.ContainingNamespace, symbol.OriginalDefinition.ContainingType, symbol.OriginalDefinition.Name, ((MethodSymbol)symbol.OriginalDefinition).Parameters);
            }
        }




        internal void StoreDetectedTasksNamespaceUsage(Enums.TasksNamespaceDetected type)
        {
            if (Enums.TasksNamespaceDetected.None != type)
            {
                switch (type)
                {
                    case Enums.TasksNamespaceDetected.TaskClass:
                        tasksNamespaceResults.NumTaskClassUsage++;
                        break;
                    case Enums.TasksNamespaceDetected.ParallelClass:
                        tasksNamespaceResults.NumParallelClassUsage++;
                        break;
                    case Enums.TasksNamespaceDetected.OtherClass:
                        tasksNamespaceResults.NumOtherClassUsage++;
                        break;
                }
            }
        }

        internal void WriteDetectedTasksNamespaceUsage(Enums.TasksNamespaceDetected type, string documentPath, ISymbol symbol, SyntaxNode node)
        {
            if (Enums.TasksNamespaceDetected.None != type)
            {
                Logger usagelog = null;
                Logger typelog = null;

                switch (type)
                {
                    case Enums.TasksNamespaceDetected.TaskClass:
                        usagelog = Logs.TempLog;
                        typelog = Logs.TempLog2;
                        break;
                    case Enums.TasksNamespaceDetected.ParallelClass:
                        usagelog = Logs.TempLog3;
                        typelog = Logs.TempLog4;
                        break;
                    case Enums.TasksNamespaceDetected.OtherClass:
                        usagelog = Logs.TempLog5;
                        typelog = Logs.TempLog6;
                        break;
                }

                SyntaxNode block = null;
                var temp = node.Ancestors().OfType<BlockSyntax>();
                if (temp.Any())
                    block = temp.First();
                else
                    block = node.Ancestors().ElementAt(3);

                typelog.Info("{0};{1}", symbol.ContainingType, symbol.ToString());
                usagelog.Info("{0} {1}\r\n{2}\r\n--------------------------", symbol, documentPath, block);


                //// Let's get rid of all specific information!
                //if (!symbol.ReturnsVoid)
                //    returntype = symbol.ReturnType.OriginalDefinition.ToString();

                //typelog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", AppName, documentPath, type.ToString(), returntype, symbol.OriginalDefinition.ContainingNamespace, symbol.OriginalDefinition.ContainingType, symbol.OriginalDefinition.Name, ((MethodSymbol)symbol.OriginalDefinition).Parameters);
            }
        }


        public static void ExtractToCsv()
        {
            int i=0;
            var SummaryJSONLogPath = ConfigurationManager.AppSettings["SummaryJSONLogPath"];
            var results = File.ReadAllLines(SummaryJSONLogPath).Select(json => JsonConvert.DeserializeObject<TaskifierAnalysisResult>(json)).ToList();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\semih\Desktop\summary.csv"))
            {
                file.WriteLine("name,total,unanalyzed,wp7,wp8,slocwp7,slocwp8,sloc,thread,threadpool,backworker,asyncdelegate,task,parallelfor, parallelforeach, parallelinvoke");
                foreach (var result in results)
                {
                    if (result.generalTaskifierResults.NumThreadUsage > 0 && result.generalTaskifierResults.NumThreadPoolUsage > 0 && result.generalTaskifierResults.NumTaskUsage > 0)
                        i += 1;

                        file.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}",
                            result.AppName,
                            result.generalResults.NumTotalProjects,
                            result.generalResults.NumUnanalyzedProjects,
                            result.generalResults.NumPhone7Projects,
                            result.generalResults.NumPhone8Projects,
                            result.generalResults.SLOCWP7,
                            result.generalResults.SLOCWP8,
                            result.generalResults.NumTotalSLOC,
                            result.generalTaskifierResults.NumThreadUsage,
                            result.generalTaskifierResults.NumThreadPoolUsage,
                            result.generalTaskifierResults.NumBackgWorkerUsage,
                            result.generalTaskifierResults.NumAsyncDelegateUsage,
                            result.generalTaskifierResults.NumTaskUsage,
                            result.generalTaskifierResults.NumParallelForUsage,
                            result.generalTaskifierResults.NumParallelForEachUsage,
                            result.generalTaskifierResults.NumParallelInvokeUsage

                            );
                    
                }

            }
            Console.WriteLine(i);
        
        }

        internal void StoreDetectedAsyncUsage(Enums.AsyncDetected type)
        {
            if (Enums.AsyncDetected.None != type)
            {
                switch (type)
                {
                    case Enums.AsyncDetected.Thread:
                        generalTaskifierResults.NumThreadUsage++;
                        break;
                    case Enums.AsyncDetected.Threadpool:
                        generalTaskifierResults.NumThreadPoolUsage++;
                        break;
                    case Enums.AsyncDetected.AsyncDelegate:
                        generalTaskifierResults.NumAsyncDelegateUsage++;
                        break;
                    case Enums.AsyncDetected.BackgroundWorker:
                        generalTaskifierResults.NumBackgWorkerUsage++;
                        break;
                    case Enums.AsyncDetected.Task:
                        generalTaskifierResults.NumTaskUsage++;
                        break;
                    case Enums.AsyncDetected.ParallelFor:
                        generalTaskifierResults.NumParallelForUsage++;
                        break;
                    case Enums.AsyncDetected.ParallelInvoke:
                        generalTaskifierResults.NumParallelInvokeUsage++;
                        break;
                    case Enums.AsyncDetected.ParallelForEach:
                        generalTaskifierResults.NumParallelForEachUsage++;
                        break;
                }
            }

        }

    }
}
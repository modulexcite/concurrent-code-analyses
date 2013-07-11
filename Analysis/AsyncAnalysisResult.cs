using System;
using System.Configuration;
using System.IO;
using Utilities;
using Roslyn.Compilers.CSharp;
using NLog;

namespace Analysis
{
    public class AsyncAnalysisResult : AnalysisResultBase
    {
        public int NumUIClasses;
        public int NumEventHandlerMethods;
        public int NumAsyncMethods;
        public int[] NumAsyncProgrammingUsages;

        protected static readonly Logger CallTraceLog = LogManager.GetLogger("CallTraceLog");

        public AsyncAnalysisResult(string appName)
            : base(appName)
        {
            NumAsyncProgrammingUsages = new int[11];

            PrintAppNameHeader(appName);
        }

        

        public override void WriteSummaryLog()
        {
            string summary= _appName + "," +
                               NumTotalProjects + "," +
                               NumUnloadedProjects + "," +
                               NumUnanalyzedProjects + "," +
                               NumPhone7Projects + "," +
                               NumPhone8Projects + "," +
                               NumNet4Projects + "," +
                               NumNet45Projects + "," +
                               NumOtherNetProjects + ",";

            foreach (var pattern in NumAsyncProgrammingUsages)
                summary+=pattern + ",";

            summary += NumAsyncMethods + "," + NumEventHandlerMethods + "," + NumUIClasses;
            SummaryLog.Info(summary);

        }





        public void WriteCallTrace(MethodDeclarationSyntax node, int n)
        {
            var path = node.SyntaxTree.FilePath;
            var start = node.GetLocation().GetLineSpan(true).StartLinePosition;

            string message="";
            for (var i = 0; i < n; i++)
                message+=" ";
            message+= node.Identifier + " " + n + " @ " + path + ":" + start;
            CallTraceLog.Info(message);
        }

        public void PrintAppNameHeader(string appName)
        {
            var header = " #################\r\n" + appName + "\r\n#################";
            CallTraceLog.Info(header);
        }

        public void PrintUnresolvedMethod(string methodCallName)
        {
            var unresolved = " //Unresolved// " + methodCallName + " \\\\\\\\\\";
            CallTraceLog.Info(unresolved);
        }

        public void PrintDispatcherOccurrence(MethodSymbol methodCallSymbol)
        {
            var dispatcher = " //GUI:Dispatcher// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(dispatcher);
        }

        public void PrintControlInvokeOccurrence(MethodSymbol methodCallSymbol)
        {
            var controlInvoke = " //GUI:Control// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(controlInvoke);
        }

        public void PrintISynchronizeInvokeOccurrence(MethodSymbol methodCallSymbol)
        {
            var text = " //GUI:ISynchronizeInvoke// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(text);
        }

        


        public void PrintEAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var eap = " //Pattern:EAP// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(eap);
        }

        public void PrintTAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var tap = " //Pattern:TAP// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(tap);
        }

        public void PrintAPMCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var apm = " //Pattern:APM// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(apm);
        }

        public void PrintThreadStartOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadStart = " //Async:Thread// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(threadStart);
        }

        public void PrintBackgroundWorkerOccurrence(MethodSymbol methodCallSymbol)
        {
            var backgroundWorker = " //Async:BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(backgroundWorker);
        }

        public void PrintThreadPoolQueueUserWorkItemOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadpool = " //Async:ThreadPool// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(threadpool);
        }
        public void PrintTPLMethodOccurrence(MethodSymbol methodCallSymbol)
        {
            var text = " //Async:TPL// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(text);
        }
        public void PrintAsyncDelegateOccurrence(MethodSymbol methodCallSymbol)
        {
            var text = " //Async:AsyncDelegate// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(text);
        }
    }
}

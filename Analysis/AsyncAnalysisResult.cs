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
        public int[] NumPatternUsages;

        protected static readonly Logger CallTraceLog = LogManager.GetLogger("CallTraceLog");

        public AsyncAnalysisResult(string appName)
            : base(appName)
        {
            NumPatternUsages = new int[11];

            PrintAppNameHeader(appName);
        }

        

        public override void WriteSummaryLog()
        {
            string summary= _appName + "," +
                               NumTotalProjects + "," +
                               NumUnloadedProjects + "," +
                               NumUnanalyzedProjects + "," +
                               NumAzureProjects + "," +
                               NumPhoneProjects + "," +
                               NumPhone8Projects + "," +
                               NumNet4Projects + "," +
                               NumNet45Projects + "," +
                               NumOtherNetProjects + ",";

            foreach (var pattern in NumPatternUsages)
                summary+=pattern + ",";

            summary += NumAsyncMethods + ", " + NumEventHandlerMethods + "," + NumUIClasses;
            SummaryLog.Info(summary);

        }





        public void WriteCallTrace(MethodDeclarationSyntax node, int n)
        {
            var path = node.SyntaxTree.FilePath;
            var start = node.Span.Start;

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
            var dispatcher = " //Dispatcher// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(dispatcher);
        }

        public void PrintControlInvokeOccurrence(MethodSymbol methodCallSymbol)
        {
            var controlInvoke = " //Form.Control// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(controlInvoke);
        }

        public void PrintEAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var eap = " //EAP// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(eap);
        }

        public void PrintTAPCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var tap = " //TAP// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(tap);
        }

        public void PrintAPMCallOccurrence(MethodSymbol methodCallSymbol)
        {
            var apm = " //APM// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(apm);
        }

        public void PrintThreadStartOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadStart = " //Thread// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(threadStart);
        }

        public void PrintBackgroundWorkerRunWorkerAsyncOccurrence(MethodSymbol methodCallSymbol)
        {
            var backgroundWorker = " //BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(backgroundWorker);
        }

        public void PrintThreadPoolQueueUserWorkItemOccurrence(MethodSymbol methodCallSymbol)
        {
            var threadpool = " //ThreadPool// " + methodCallSymbol + " \\\\\\\\\\";
            CallTraceLog.Info(threadpool);
        }

        public void PrintThreadPoolQueueUserWorkItemWithSynchronizationContextOccurrence(InvocationExpressionSyntax methodCall)
        {
            var threadpoolContext = " //ThreadPool with Context// " + methodCall + " \\\\\\\\\\";
            CallTraceLog.Info(threadpoolContext);
        }

        public void PrintThreadPoolQueueUserWorkItemWithDispatcherOccurrence(InvocationExpressionSyntax methodCall)
        {
            var threadpoolDispatcher = " //ThreadPool with Dispatcher// " + methodCall + " \\\\\\\\\\";
            CallTraceLog.Info(threadpoolDispatcher);
        }
    }
}

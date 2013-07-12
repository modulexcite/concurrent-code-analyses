using System;
using System.Configuration;
using System.IO;
using Utilities;
using Roslyn.Compilers.CSharp;
using NLog;
using Roslyn.Services;

namespace Analysis
{
    public class AsyncAnalysisResult : AnalysisResultBase
    {
        public int NumUIClasses;
        public int NumEventHandlerMethods;
        public int NumAsyncVoidNonEventHandlerMethods;
        public int NumAsyncVoidEventHandlerMethods;
        public int NumAsyncTaskMethods;

        public int[] NumAsyncProgrammingUsages;

        protected static readonly Logger CallTraceLog = LogManager.GetLogger("CallTraceLog");
        protected static readonly Logger ClassifierLog = LogManager.GetLogger("ClassifierLog");
        
        
        
        public AsyncAnalysisResult(string appName)
            : base(appName)
        {
            NumAsyncProgrammingUsages = new int[11];
        }

        public void StoreDetectedAsyncUsage(AsyncAnalysis.Detected type)
        {
            if (AsyncAnalysis.Detected.None != type)
                NumAsyncProgrammingUsages[(int)type]++;
        }

        public override void WriteSummaryLog()
        {
            string summary = _appName + "," +
                               NumTotalProjects + "," +
                               NumUnloadedProjects + "," +
                               NumUnanalyzedProjects + "," +
                               NumPhone7Projects + "," +
                               NumPhone8Projects + "," +
                               NumNet4Projects + "," +
                               NumNet45Projects + "," +
                               NumOtherNetProjects + "," +
                               NumTotalSLOC+ ",";

            foreach (var pattern in NumAsyncProgrammingUsages)
                summary+=pattern + ",";

            summary += NumAsyncVoidNonEventHandlerMethods + "," + NumAsyncVoidEventHandlerMethods+ ","+ NumAsyncTaskMethods +","+ NumEventHandlerMethods + "," + NumUIClasses;
            SummaryLog.Info(summary);

        }




        public void WriteNodeToCallTrace(MethodDeclarationSyntax node, int n)
        {
            var path = node.SyntaxTree.FilePath;
            var start = node.GetLocation().GetLineSpan(true).StartLinePosition;

            string message="";
            for (var i = 0; i < n; i++)
                message+=" ";
            message+= node.Identifier + " " + n + " @ " + path + ":" + start;
            CallTraceLog.Info(message);
        }

        public void WriteDetectedAsyncToCallTrace(AsyncAnalysis.Detected type, string methodCall) 
        {

            if (AsyncAnalysis.Detected.None != type)
            {
                var text = "///" + type.ToString() + "///  " + methodCall;
                CallTraceLog.Info(text);
            }
        }

        public void WriteDetectedAsync(AsyncAnalysis.Detected type,  string documentPath, MethodSymbol symbol)
        {
            if (AsyncAnalysis.Detected.None != type)
            {
                var methodCallString = symbol.ToString(); ;
                if (symbol.ReturnsVoid)
                    methodCallString = "void " + methodCallString;
                else
                    methodCallString = symbol.ReturnType.ToString() + " " + methodCallString;

                ClassifierLog.Info(@"{0},{1},{2},{3}", _appName, documentPath, methodCallString.Replace(",", ";"), type.ToString());
            }
        }  
    }
}

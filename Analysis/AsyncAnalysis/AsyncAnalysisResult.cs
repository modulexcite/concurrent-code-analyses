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

        public void StoreDetectedAsyncUsage(Enums.Detected type)
        {
            if (Enums.Detected.None != type)
                NumAsyncProgrammingUsages[(int)type]++;
        }

        public override void WriteSummaryLog()
        {
            string summary = _appName + "," +
                               NumTotalProjects + "," +
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

        public void WriteDetectedAsyncToCallTrace(Enums.Detected type, MethodSymbol symbol) 
        {
            if (Enums.Detected.None != type)
            {
                var text = "///" + type.ToString() + "///  " + symbol.ToStringWithReturnType();
                CallTraceLog.Info(text);
            }
        }

        public void WriteDetectedAsync(Enums.Detected type, string documentPath, MethodSymbol symbol)
        {
            if (Enums.Detected.None != type)
            {
                string returntype;
                if (symbol.ReturnsVoid)
                    returntype = "void ";
                else
                    returntype = symbol.ReturnType.ToString();
                
                //Class name: symbol.ContainingType.ToString()
                
                ClassifierLog.Info(@"{0},{1},{2},{3}", _appName, documentPath.Replace(",",";"), returntype.Replace(",",";"), symbol.OriginalDefinition.ToString().Replace(",", ";"), type.ToString());
            }
        }  
    }
}

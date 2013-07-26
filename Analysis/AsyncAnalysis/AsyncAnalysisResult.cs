using NLog;
using Roslyn.Compilers.CSharp;
using Utilities;

namespace Analysis
{
    public class AsyncAnalysisResult : AnalysisResultBase
    {
        public class GeneralAsyncResults
        {
            public int NumUIClasses;
            public int NumEventHandlerMethods;
        }

        public class AsyncAwaitResults
        {
            public int NumAsyncVoidNonEventHandlerMethods;
            public int NumAsyncVoidEventHandlerMethods;
            public int NumAsyncTaskMethods;
            public int NumAsyncMethodsHavingConfigureAwait;
            public int NumAsyncMethodsHavingBlockingCalls;
            public int NumAsyncMethodsNotHavingAwait;
        }

        public class APMDiagnosisResults
        {
            public int NumAPMBeginMethods;
            public int NumAPMBeginFollowed;
            public int NumAPMEndMethods;
            public int NumAPMEndTryCatchedMethods;
            public int NumAPMEndNestedMethods;
        }

        public class SyncUsageResults
        {
            public int NumSyncReplacableUsages;
            public int NumGUIBlockingSyncUsages;
        }

        public class AsyncUsageResults
        {
            public int[] NumAsyncProgrammingUsages = new int[11];
        }

        public GeneralAsyncResults generalAsyncResults { get; set; }

        public AsyncAwaitResults asyncAwaitResults { get; set; }

        public APMDiagnosisResults apmDiagnosisResults { get; set; }

        public SyncUsageResults syncUsageResults { get; set; }

        public AsyncUsageResults asyncUsageResults { get; set; }

        protected static readonly Logger CallTraceLog = LogManager.GetLogger("CallTraceLog");
        protected static readonly Logger SyncClassifierLog = LogManager.GetLogger("SyncClassifierLog");
        protected static readonly Logger AsyncClassifierLog = LogManager.GetLogger("AsyncClassifierLog");
        protected static readonly Logger AsyncClassifierOriginalLog = LogManager.GetLogger("AsyncClassifierOriginalLog");

        public AsyncAnalysisResult(string appName)
            : base(appName)
        {
            generalAsyncResults = new GeneralAsyncResults();
            asyncAwaitResults = new AsyncAwaitResults();
            apmDiagnosisResults = new APMDiagnosisResults();
            syncUsageResults = new SyncUsageResults();
            asyncUsageResults = new AsyncUsageResults();
        }

        public void StoreDetectedAsyncUsage(Enums.AsyncDetected type)
        {
            if (Enums.AsyncDetected.None != type)
                asyncUsageResults.NumAsyncProgrammingUsages[(int)type]++;
        }

        public override void WriteSummaryLog()
        {
            //string summary = String.Format(@"{0},{1},{2},{3},{4},{5},{6},{7},{8}",
            //                       _appName,
            //                       NumTotalProjects,
            //                       NumUnanalyzedProjects,
            //                       NumPhone7Projects,
            //                       NumPhone8Projects,
            //                       NumNet4Projects,
            //                       NumNet45Projects,
            //                       NumOtherNetProjects,
            //                       NumTotalSLOC);

            //foreach (var pattern in NumAsyncProgrammingUsages)
            //    summary+=pattern + ",";

            //summary += String.Format(@"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},",
            //    NumGUIBlockingSyncUsages,
            //    NumSyncReplacableUsages,

            //    NumAsyncMethodsHavingConfigureAwait,
            //    NumAsyncMethodsHavingBlockingCalls,
            //    NumAsyncMethodsNotHavingAwait,

            //    NumAsyncVoidNonEventHandlerMethods,
            //    NumAsyncVoidEventHandlerMethods,
            //    NumAsyncTaskMethods,

            //    NumEventHandlerMethods,
            //    NumUIClasses);

            //summary += String.Format(@"{0},{1},{2},{3},{4}",
            //    NumAPMBeginMethods,
            //    NumAPMBeginFollowed,
            //    NumAPMEndMethods,
            //    NumAPMEndTryCatchedMethods,
            //    NumAPMEndNestedMethods);

            //SummaryLog.Info(summary);
        }

        public void WriteNodeToCallTrace(MethodDeclarationSyntax node, int n)
        {
            var path = node.SyntaxTree.FilePath;
            var start = node.GetLocation().GetLineSpan(true).StartLinePosition;

            string message = "";
            for (var i = 0; i < n; i++)
                message += " ";
            message += node.Identifier + " " + n + " @ " + path + ":" + start;
            CallTraceLog.Info(message);
        }

        internal void WriteDetectedAsyncToCallTrace(Enums.AsyncDetected type, MethodSymbol symbol)
        {
            if (Enums.AsyncDetected.None != type)
            {
                var text = "///" + type.ToString() + "///  " + symbol.ToStringWithReturnType();
                CallTraceLog.Info(text);
            }
        }

        internal void WriteDetectedAsyncUsage(Enums.AsyncDetected type, string documentPath, MethodSymbol symbol)
        {
            if (Enums.AsyncDetected.None != type)
            {
                string returntype;
                if (symbol.ReturnsVoid)
                    returntype = "void ";
                else
                    returntype = symbol.ReturnType.ToString();

                AsyncClassifierLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", generalResults.AppName, documentPath, type.ToString(), returntype, symbol.ContainingNamespace, symbol.ContainingType, symbol.Name, symbol.Parameters);

                // Let's get rid of all generic information!

                if (!symbol.ReturnsVoid)
                    returntype = symbol.ReturnType.OriginalDefinition.ToString();

                AsyncClassifierOriginalLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", generalResults.AppName, documentPath, type.ToString(), returntype, symbol.OriginalDefinition.ContainingNamespace, symbol.OriginalDefinition.ContainingType, symbol.OriginalDefinition.Name, ((MethodSymbol)symbol.OriginalDefinition).Parameters);
            }
        }

        internal void StoreDetectedSyncUsage(Enums.SyncDetected synctype)
        {
            if (Enums.SyncDetected.None != synctype)
                syncUsageResults.NumSyncReplacableUsages++;
        }

        internal void WriteDetectedSyncUsage(Enums.SyncDetected type, string documentPath, MethodSymbol symbol)
        {
            if (Enums.SyncDetected.None != type)
            {
                string returntype;
                if (symbol.ReturnsVoid)
                    returntype = "void ";
                else
                    returntype = symbol.ReturnType.ToString();

                SyncClassifierLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", generalResults.AppName, documentPath, type.ToString(), returntype, symbol.ContainingNamespace, symbol.ContainingType, symbol.Name, symbol.Parameters);
            }
        }
    }
}
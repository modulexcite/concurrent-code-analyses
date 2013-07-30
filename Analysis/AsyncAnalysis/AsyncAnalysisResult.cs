using Newtonsoft.Json;
using NLog;
using Roslyn.Compilers.CSharp;
using System;
using System.Configuration;
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
            Logs.SummaryJSONLog.Info(@"{0}", JsonConvert.SerializeObject(this, Formatting.None));
        }
        public bool ShouldSerializeasyncAwaitResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsAsyncAwaitDetectionEnabled"]);
        }

        public bool ShouldSerializeapmDiagnosisResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsAPMDiagnosisDetectionEnabled"]);
        }

        public bool ShouldSerializesyncUsageResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsSyncUsageDetectionEnabled"]);
        }

        public bool ShouldSerializeasyncUsageResults()
        { 
            return bool.Parse(ConfigurationManager.AppSettings["IsAsyncUsageDetectionEnabled"]);
        }

        public bool ShouldSerializegeneralAsyncResults()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsGeneralAsyncDetectionEnabled"]);
        }


        

        public void WriteNodeToCallTrace(MethodDeclarationSyntax node, int n)
        {
            var path = node.SyntaxTree.FilePath;
            var start = node.GetLocation().GetLineSpan(true).StartLinePosition;

            string message = "";
            for (var i = 0; i < n; i++)
                message += " ";
            message += node.Identifier + " " + n + " @ " + path + ":" + start;
            Logs.CallTraceLog.Info(message);
        }

        internal void WriteDetectedAsyncToCallTrace(Enums.AsyncDetected type, MethodSymbol symbol)
        {
            if (Enums.AsyncDetected.None != type)
            {
                var text = "///" + type.ToString() + "///  " + symbol.ToStringWithReturnType();
                Logs.CallTraceLog.Info(text);
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

                Logs.AsyncClassifierLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", AppName, documentPath, type.ToString(), returntype, symbol.ContainingNamespace, symbol.ContainingType, symbol.Name, symbol.Parameters);

                // Let's get rid of all generic information!

                if (!symbol.ReturnsVoid)
                    returntype = symbol.ReturnType.OriginalDefinition.ToString();

                Logs.AsyncClassifierOriginalLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", AppName, documentPath, type.ToString(), returntype, symbol.OriginalDefinition.ContainingNamespace, symbol.OriginalDefinition.ContainingType, symbol.OriginalDefinition.Name, ((MethodSymbol)symbol.OriginalDefinition).Parameters);
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

                Logs.SyncClassifierLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", AppName, documentPath, type.ToString(), returntype, symbol.ContainingNamespace, symbol.ContainingType, symbol.Name, symbol.Parameters);
            }
        }
    }
}
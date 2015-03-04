using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using Utilities;
using System.Linq;

namespace Analysis
{
    public class AsyncAnalysisResult : AnalysisResult
    {
        public class GeneralAsyncResults
        {
            public int NumUIClasses;
            public int NumEventHandlerMethods;
        }

        public class AsyncAwaitResults
        {
            public int NumAsyncAwaitMethods_WP7;
            public int NumAsyncAwaitMethods_WP8;

            public int NumAsyncVoidNonEventHandlerMethods;
            public int NumAsyncVoidEventHandlerMethods;
            public int NumAsyncTaskMethods;
            public int NumAsyncMethodsHavingConfigureAwait;
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
            public int NumAsyncAwaitMethods;
            public int[] NumAsyncProgrammingUsages = new int[11];
        }

        public GeneralAsyncResults generalAsyncResults { get; set; }

        public AsyncAwaitResults asyncAwaitResults { get; set; }

        public APMDiagnosisResults apmDiagnosisResults { get; set; }

        public SyncUsageResults syncUsageResults { get; set; }

        public AsyncUsageResults asyncUsageResults_WP7 { get; set; }

        public AsyncUsageResults asyncUsageResults_WP8 { get; set; }

        public string commit;
        public AsyncAnalysisResult(string solutionPath, string appName)
            : base(solutionPath, appName)
        {
            generalAsyncResults = new GeneralAsyncResults();
            asyncAwaitResults = new AsyncAwaitResults();
            apmDiagnosisResults = new APMDiagnosisResults();
            syncUsageResults = new SyncUsageResults();
            asyncUsageResults_WP7 = new AsyncUsageResults();
            asyncUsageResults_WP8 = new AsyncUsageResults();
            var tmp = File.ReadLines(@"C:\Users\Semih\Desktop\commitMap.txt").Where(line => line.Contains(appName.Replace('+', '/')));
            if(tmp.Any())
                commit = tmp.First().Split(',')[1];


        }

        public void StoreDetectedAsyncUsage(Enums.AsyncDetected type)
        {
            if (Enums.AsyncDetected.None != type)
            {
                if(CurrentAnalyzedProjectType== Enums.ProjectType.WP7)
                    asyncUsageResults_WP7.NumAsyncProgrammingUsages[(int)type]++;
                else
                    asyncUsageResults_WP8.NumAsyncProgrammingUsages[(int)type]++;
            }
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

        public bool ShouldSerializeasyncUsageResults_WP7()
        {
            return bool.Parse(ConfigurationManager.AppSettings["IsAsyncUsageDetectionEnabled"]);
        }

        public bool ShouldSerializeasyncUsageResults_WP8()
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
            var start = node.GetLocation().GetLineSpan().StartLinePosition;

            string message = "";
            for (var i = 0; i < n; i++)
                message += " ";
            message += node.Identifier + " " + n + " @ " + path + ":" + start;
            Logs.CallTraceLog.Info(message);
        }

        internal void WriteDetectedAsyncToCallTrace(Enums.AsyncDetected type, IMethodSymbol symbol)
        {
            if (Enums.AsyncDetected.None != type)
            {
                var text = "///" + type.ToString() + "///  " + symbol.ToStringWithReturnType();
                Logs.CallTraceLog.Info(text);
            }
        }

        internal void WriteDetectedAsyncUsage(Enums.AsyncDetected type, string documentPath, IMethodSymbol symbol)
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

                Logs.AsyncClassifierOriginalLog.Info(@"{0};{1};{2};{3};{4};{5};{6};{7}", AppName, documentPath, type.ToString(), returntype, symbol.OriginalDefinition.ContainingNamespace, symbol.OriginalDefinition.ContainingType, symbol.OriginalDefinition.Name, ((IMethodSymbol)symbol.OriginalDefinition).Parameters);
            }
        }

        internal void StoreDetectedSyncUsage(Enums.SyncDetected synctype)
        {
            if (Enums.SyncDetected.None != synctype)
                syncUsageResults.NumSyncReplacableUsages++;
        }

        internal void WriteDetectedSyncUsage(Enums.SyncDetected type, string documentPath, IMethodSymbol symbol)
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

        internal void WriteDetectedAsyncUsageToTable(Enums.AsyncDetected type, Microsoft.CodeAnalysis.Document Document, IMethodSymbol symbol, InvocationExpressionSyntax node)
        {
            if (Enums.AsyncDetected.None != type)
            {
                string resultfile = @"C:\Users\Semih\Desktop\Tables\" + (int)type + ".txt";
                string delimStr = "/";
                char[] delimiter = delimStr.ToCharArray();
                var location = node.GetLocation().GetLineSpan();
                var app= AppName.Replace("+","/");

                string filepathWithLineNumbers = "http://www.github.com/"+ app +"/blob/" + commit +"/"+
                    Document.FilePath.Replace(@"\",@"/").Split(delimiter,6)[5] +"#L"+(location.StartLinePosition.Line+1)+"-"+(location.EndLinePosition.Line+1);
                var row = String.Format(@"<tr> <td>{0}</td><td>{1}</td><td><a href='{2}'>Link to Source Code</a></td> </tr>", app, symbol, filepathWithLineNumbers);

                using (StreamWriter sw = File.AppendText(resultfile))
                {
                    sw.WriteLine(row);
                }	

            }
        }


        internal void WriteDetectedMisuseAsyncUsageToTable(int type, Microsoft.CodeAnalysis.Document Document, MethodDeclarationSyntax node)
        {

                string resultfile = @"C:\Users\Semih\Desktop\MisuseTables\" + type + ".txt";
                string delimStr = "/";
                char[] delimiter = delimStr.ToCharArray();
                var location = node.GetLocation().GetLineSpan();
                var app = AppName.Replace("+", "/");

                var methodName= node.Modifiers.ToString()+ " "+ node.ReturnType.ToString()+ " "+ node.Identifier.ToString() + " "+ node.ParameterList.ToString();

                string filepathWithLineNumbers = "http://www.github.com/" + app + "/blob/" + commit + "/" +
                    Document.FilePath.Replace(@"\", @"/").Split(delimiter, 6)[5] + "#L" + (location.StartLinePosition.Line + 1) + "-" + (location.EndLinePosition.Line + 1);
                var row = String.Format(@"<tr><td>{0}</td><td><a href='{1}'>Link to Source Code</a></td> </tr>", methodName, filepathWithLineNumbers);

                using (StreamWriter sw = File.AppendText(resultfile))
                {
                    sw.WriteLine(row);
                }
        }
    }
}
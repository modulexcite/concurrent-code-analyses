using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.IO;
using Microsoft.Build.Exceptions;
using Utilities;

namespace Analysis
{
    public class AsyncAnalysis : AnalysisBase
    {

        private AsyncAnalysisResult result;
        public override AnalysisResultBase ResultObject
        {
            get { return result; }
        }
        public new AsyncAnalysisResult Result
        {
            get { return result; }
        }

        public AsyncAnalysis(string dirName, string appName)
            : base(dirName,appName)
        {
            result = new AsyncAnalysisResult(appName);
        }


        protected override void AnalyzeDocument(IDocument document)
        {
            var root = (SyntaxNode)  document.GetSyntaxTree().GetRoot();
            SemanticModel semanticModel= (SemanticModel) document.GetSemanticModel();
            SyntaxWalker walker;
            
            walker = new EventHandlerMethodsWalker()
            {
                Analysis = this,
                Result = Result,
            };


            walker = new InvocationsWalker()
            {
                Analysis = this,
                Result = Result,
                SemanticModel = semanticModel,
            };
            

            walker.Visit(root);
        }



        public void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            var newMethods = new List<MethodDeclarationSyntax>();
            Result.WriteCallTrace(node, n);

            var doc = CurrentSolution.GetDocument(node.SyntaxTree.GetRoot().SyntaxTree);

            try
            {
                var semanticModel = doc.GetSemanticModel();
                foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var methodCallSymbol = (MethodSymbol)((SemanticModel)semanticModel).GetSymbolInfo(methodCall).Symbol;

                    DetectAsyncProgrammingUsages(methodCall, methodCallSymbol);

                    var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();

                    // go down only 3 deep levels
                    if (methodDeclarationNode != null && n < 3 && methodDeclarationNode != node)
                        newMethods.Add(methodDeclarationNode);
                }

                foreach (var newMethod in newMethods)
                    ProcessMethodCallsInMethod(newMethod, n + 1);
            }
            catch (Exception ex)
            {
                Log.Warn("Caught exception while processing method call node: {0} @ {1}:{2}", node, doc.FilePath, node.Span.Start, ex);

                if (!(ex is InvalidProjectFileException ||
                      ex is FormatException ||
                      ex is ArgumentException ||
                      ex is PathTooLongException))
                    throw;
            }
        }




        private void DetectAsyncProgrammingUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            if (methodCallSymbol == null)
            {
                if (methodCallName.Contains("begininvoke") || methodCallName.Contains("async"))
                {
                    Result.NumAsyncProgrammingUsages[11]++;
                    Result.PrintUnresolvedMethod(methodCallName);
                }
                return;
            }

            // DETECT PATTERNS
            if (methodCallSymbol.IsAPMBeginMethod())
            {
                Result.PrintAPMCallOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[0]++;
            }
            else if (methodCall.IsEAPMethod())
            {
                Result.PrintEAPCallOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[1]++;
            }
            else if (methodCallSymbol.IsTAPMethod())
            {
                Result.PrintTAPCallOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[2]++;
            }

            // DETECT ASYNC CALLS
            else if (methodCallSymbol.IsThreadStart())
            {
                Result.PrintThreadStartOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[3]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
            {
                Result.PrintThreadPoolQueueUserWorkItemOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[4]++;
            }
            else if (methodCallSymbol.IsAsyncDelegate())
            {
                Result.PrintAsyncDelegateOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[5]++;
            }
            else if (methodCallSymbol.IsBackgroundWorkerMethod())
            {
                Result.PrintBackgroundWorkerOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[6]++;
            }
            else if (methodCallSymbol.IsTPLMethod())
            {
                Result.PrintTPLMethodOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[7]++;
            }

            // DETECT GUI UPDATE CALLS
            else if (methodCallSymbol.IsISynchronizeInvokeMethod())
            {
                Result.PrintISynchronizeInvokeOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[8]++;
            }
            else if (methodCallSymbol.IsControlBeginInvoke())
            {
                Result.PrintControlInvokeOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[9]++;
            }
            else if (methodCallSymbol.IsDispatcherBeginInvoke())
            {
                Result.PrintDispatcherOccurrence(methodCallSymbol);
                Result.NumAsyncProgrammingUsages[10]++;
            }

            
        }
    }
}

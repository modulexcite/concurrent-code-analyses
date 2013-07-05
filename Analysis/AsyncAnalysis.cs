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
            var syntaxTree = document.GetSyntaxTree();
            var loopWalker = new AsyncAnalysisWalker(this, Result);

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
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

                    DetectAsyncPatternUsages(methodCall, methodCallSymbol);

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

        private void DetectAsyncPatternUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            if (methodCallSymbol == null)
            {
                if (methodCallName.Contains("begininvoke") || methodCallName.Contains("async"))
                {
                    Result.NumPatternUsages[10]++;
                    Result.PrintUnresolvedMethod(methodCallName);
                }
                return;
            }

            if (methodCallSymbol.IsDispatcherBeginInvoke())
            {
                Result.PrintDispatcherOccurrence(methodCallSymbol);
                Result.NumPatternUsages[0]++;
            }
            else if (methodCallSymbol.IsControlBeginInvoke())
            {
                Result.PrintControlInvokeOccurrence(methodCallSymbol);
                Result.NumPatternUsages[1]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsBeginInvoke())
            {
                Result.PrintThreadPoolQueueUserWorkItemWithDispatcherOccurrence(methodCall);
                Result.NumPatternUsages[2]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsSynchronizationContext()) // look at the synchronization context
            {
                Result.PrintThreadPoolQueueUserWorkItemWithSynchronizationContextOccurrence(methodCall);
                Result.NumPatternUsages[3]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
            {
                Result.PrintThreadPoolQueueUserWorkItemOccurrence(methodCallSymbol);
                Result.NumPatternUsages[4]++;
            }
            else if (methodCallSymbol.IsBackgroundWorkerRunWorkerAsync())
            {
                Result.PrintBackgroundWorkerRunWorkerAsyncOccurrence(methodCallSymbol);
                Result.NumPatternUsages[5]++;
            }
            else if (methodCallSymbol.IsThreadStart())
            {
                Result.PrintThreadStartOccurrence(methodCallSymbol);
                Result.NumPatternUsages[6]++;
            }
            else if (methodCallSymbol.IsAPMBeginMethod())
            {
                Result.PrintAPMCallOccurrence(methodCallSymbol);
                Result.NumPatternUsages[7]++;
            }
            else if (methodCallSymbol.ReturnsTask())
            {
                Result.PrintTAPCallOccurrence(methodCallSymbol);
                Result.NumPatternUsages[8]++;
            }
            else if (methodCall.IsEAPMethod())
            {
                Result.PrintEAPCallOccurrence(methodCallSymbol);
                Result.NumPatternUsages[9]++;
            }
        }
    }
}

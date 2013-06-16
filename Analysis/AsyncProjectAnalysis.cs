using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.IO;
using Microsoft.Build.Exceptions;
using Utilities;

namespace Analysis
{
    public class AsyncProjectAnalysis : ProjectAnalysisBase
    {
        private const string InterestingCallsFile = @"C:\Users\david\Desktop\callsFromEventHandlers.txt";

        private readonly AsyncProjectAnalysisSummary _summary;

        public AsyncProjectAnalysis(string appName, string dirName, AsyncProjectAnalysisSummary summary)
            : base(dirName, summary)
        {
            _summary = summary;
            Helper.WriteLogger(InterestingCallsFile, " #################\r\n" + appName + "\r\n#################\r\n");
        }

        public override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();
            var loopWalker = new AsyncAnalysisWalker(this, _summary);

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        public override void OnAnalysisCompleted()
        {
            _summary.WriteResults();
        }

        public void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            var newMethods = new List<MethodDeclarationSyntax>();
            for (var i = 0; i < n; i++)
                Helper.WriteLogger(InterestingCallsFile, " ");
            Helper.WriteLogger(InterestingCallsFile, node.Identifier + " " + n + "\r\n");

            var doc = CurrentSolution.GetDocument(node.SyntaxTree.GetRoot().SyntaxTree);

            try
            {
                var semanticModel = doc.GetSemanticModel();
                foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var methodCallSymbol = (MethodSymbol)((SemanticModel)semanticModel).GetSymbolInfo(methodCall).Symbol;

                    DetectAsyncPatternUsages(methodCall, methodCallSymbol);

                    var methodDeclarationNode = FindMethodDeclarationNode(methodCallSymbol);

                    if (methodDeclarationNode != null && n < 5 && methodDeclarationNode != node)
                        newMethods.Add(methodDeclarationNode);
                }

                foreach (var newMethod in newMethods)
                    ProcessMethodCallsInMethod(newMethod, n + 1);

            }
            catch (Exception ex)
            {
                if (!(ex is InvalidProjectFileException ||
                      ex is FormatException ||
                      ex is ArgumentException ||
                      ex is PathTooLongException))
                    throw;
            }
        }

        private static MethodDeclarationSyntax FindMethodDeclarationNode(MethodSymbol methodCallSymbol)
        {
            if (methodCallSymbol == null)
                return null;

            var nodes = methodCallSymbol.DeclaringSyntaxNodes;

            if (nodes == null || nodes.Count == 0)
                return null;

            if (nodes.First() is MethodDeclarationSyntax)
                return (MethodDeclarationSyntax)nodes.First();

            return null;
        }

        public void DetectAsyncPatternUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            if (methodCallSymbol == null)
            {
                if (methodCallName.Contains("begininvoke") || methodCallName.Contains("async"))
                {
                    _summary.NumPatternUsages[10]++;
                    Helper.WriteLogger(InterestingCallsFile, " //Unresolved// " + methodCallName + " \\\\\\\\\\\r\n");
                }
                return;
            }

            var methodSymbolString = methodCallSymbol.ToString();

            if (methodCallSymbol.IsDispatcherBeginInvoke())
            {
                Helper.WriteLogger(InterestingCallsFile, " //Dispatcher// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[0]++;
            }
            else if (methodCallSymbol.IsControlBeginInvoke())
            {
                Helper.WriteLogger(InterestingCallsFile, " //Form.Control// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[1]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsBeginInvoke()) // look at the synchronization context
            {
                Helper.WriteLogger(InterestingCallsFile, " //ThreadPool with Dispatcher// " + methodCall + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[2]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsSynchronizationContext()) // look at the synchronization context
            {
                Helper.WriteLogger(InterestingCallsFile, " //ThreadPool with Context// " + methodCall + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[3]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
            {
                Helper.WriteLogger(InterestingCallsFile, " //ThreadPool// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[4]++;
            }
            else if (methodCallSymbol.IsBackgroundWorkerRunWorkerAsync())
            {
                Helper.WriteLogger(InterestingCallsFile, " //BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[5]++;
            }
            else if (methodCallSymbol.IsThreadStart())
            {
                Helper.WriteLogger(InterestingCallsFile, " //Thread// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[6]++;
            }
            else if (methodSymbolString.Contains("System.IAsyncResult") || (methodCallSymbol.ReturnsIAsyncResult()))
            {
                Helper.WriteLogger(InterestingCallsFile, " //APM// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[7]++;
            }
            else if (methodCallSymbol.ReturnsTask())
            {
                Helper.WriteLogger(InterestingCallsFile, " //TAP// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                _summary.NumPatternUsages[8]++;
            }
            else if (methodCall.CallsAsyncMethod() && methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First().IsEAPCompletedMethod())
            {
                Helper.WriteLogger(InterestingCallsFile, " //EAP// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                Helper.WriteLogger(@"C:\Users\david\Desktop\temp.txt", methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First() + "\\\\\\\\\\\r\n");
                _summary.NumPatternUsages[9]++;
            }
        }
    }
}

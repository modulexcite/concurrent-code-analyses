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
    public class AsyncProjectAnalysis : ProjectAnalysisBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly AsyncProjectAnalysisSummary _summary;
        private readonly InterestingCallsCollector _interestingCalls;
        private readonly StreamWriter _interestingCallsWriter;
        private readonly StreamWriter _callTraceWriter;

        public AsyncProjectAnalysis(string dirName, AsyncProjectAnalysisSummary summary, StreamWriter interestingCallsWriter, StreamWriter callTraceWriter)
            : base(dirName, summary)
        {
            _summary = summary;
            _interestingCalls = new InterestingCallsCollector(interestingCallsWriter);
            _interestingCallsWriter = interestingCallsWriter;
            _callTraceWriter = callTraceWriter;

            _interestingCalls.PrintAppNameHeader(_summary.AppName);
        }

        protected override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();
            var loopWalker = new AsyncAnalysisWalker(this, _summary);

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        protected override void OnAnalysisCompleted()
        {
            _summary.WriteResults();
        }

        public void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            var newMethods = new List<MethodDeclarationSyntax>();
            _interestingCalls.WriteCallTrace(node, n);

            var doc = CurrentSolution.GetDocument(node.SyntaxTree.GetRoot().SyntaxTree);

            try
            {
                var semanticModel = doc.GetSemanticModel();
                foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var methodCallSymbol = (MethodSymbol)((SemanticModel)semanticModel).GetSymbolInfo(methodCall).Symbol;

                    DetectAsyncPatternUsages(methodCall, methodCallSymbol);

                    var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();

                    if (methodDeclarationNode != null && n < 5 && methodDeclarationNode != node)
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
                    _summary.NumPatternUsages[10]++;
                    _interestingCalls.PrintUnresolvedMethod(methodCallName);
                }
                return;
            }

            var methodSymbolString = methodCallSymbol.ToString();

            if (methodCallSymbol.IsDispatcherBeginInvoke())
            {
                _interestingCalls.PrintDispatcherOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[0]++;
            }
            else if (methodCallSymbol.IsControlBeginInvoke())
            {
                _interestingCalls.PrintControlInvokeOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[1]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsBeginInvoke()) // look at the synchronization context
            {
                _interestingCalls.PrintThreadPoolQueueUserWorkItemWithDispatcherOccurrence(methodCall);
                _summary.NumPatternUsages[2]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsSynchronizationContext()) // look at the synchronization context
            {
                _interestingCalls.PrintThreadPoolQueueUserWorkItemWithSynchronizationContextOccurrence(methodCall);
                _summary.NumPatternUsages[3]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
            {
                _interestingCalls.PrintThreadPoolQueueUserWorkItemOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[4]++;
            }
            else if (methodCallSymbol.IsBackgroundWorkerRunWorkerAsync())
            {
                _interestingCalls.PrintBackgroundWorkerRunWorkerAsyncOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[5]++;
            }
            else if (methodCallSymbol.IsThreadStart())
            {
                _interestingCalls.PrintThreadStartOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[6]++;
            }
            else if (methodSymbolString.Contains("System.IAsyncResult") || (methodCallSymbol.ReturnsIAsyncResult()))
            {
                _interestingCalls.PrintAPMCallOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[7]++;
            }
            else if (methodCallSymbol.ReturnsTask())
            {
                _interestingCalls.PrintTAPCallOccurrence(methodCallSymbol);
                _summary.NumPatternUsages[8]++;
            }
            else if (methodCall.CallsAsyncMethod() && methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First().IsEAPCompletedMethod())
            {
                _interestingCalls.PrintEAPCallOccurrence(methodCallSymbol);
                WriteCallTraceToTempFile(methodCall);
                _summary.NumPatternUsages[9]++;
            }
        }

        private void WriteCallTraceToTempFile(InvocationExpressionSyntax methodCall)
        {
            _callTraceWriter.Write(methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First() + "\\\\\\\\\\\r\n");
        }
    }
}

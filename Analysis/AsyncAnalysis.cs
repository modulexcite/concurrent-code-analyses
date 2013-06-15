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
    public class AsyncAnalysis : AnalysisBase
    {
        private const string AppsFile = @"C:\Users\david\Desktop\UIStatistics.txt";
        private const string InterestingCallsFile = @"C:\Users\david\Desktop\callsFromEventHandlers.txt";

        public int NumUIClasses;
        public int NumEventHandlerMethods;
        public int NumAsyncMethods;
        public int[] NumPatternUsages;

        private AsyncAnalysisSummary _summary;

        public AsyncAnalysis(string appName, string dirName, AsyncAnalysisSummary summary)
            : base(appName, dirName, summary)
        {
            _summary = summary;
            Helper.WriteLogger(InterestingCallsFile, " #################\r\n" + appName + "\r\n#################\r\n");
            NumPatternUsages = new int[11];
        }

        public override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();
            var loopWalker = new Walker(this);

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        public override void OnAnalysisCompleted()
        {
            Helper.WriteLogger(AppsFile,
                AppName + "," +
                NumTotalProjects + "," +
                NumUnloadedProjects + "," +
                NumUnanalyzedProjects + "," +
                _summary.NumAzureProjects + "," +
                _summary.NumPhoneProjects + "," +
                _summary.NumPhone8Projects + "," +
                _summary.NumNet4Projects + "," +
                _summary.NumNet45Projects + "," +
                _summary.NumOtherNetProjects + ",");

            foreach (var pattern in NumPatternUsages)
                Helper.WriteLogger(AppsFile, pattern + ",");

            Helper.WriteLogger(AppsFile, NumAsyncMethods + ", " + NumEventHandlerMethods + "," + NumUIClasses + "\r\n");
        }

        public void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            var newMethods = new List<MethodDeclarationSyntax>();
            for (var i = 0; i < n; i++)
                Helper.WriteLogger(InterestingCallsFile, " "); ;
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
                if (ex is InvalidProjectFileException ||
                        ex is FormatException ||
                        ex is ArgumentException ||
                        ex is PathTooLongException)
                    return;
                else
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
                    NumPatternUsages[10]++;
                    Helper.WriteLogger(InterestingCallsFile, " //Unresolved// " + methodCallName + " \\\\\\\\\\\r\n");
                }
                return;
            }

            var methodSymbolString = methodCallSymbol.ToString();

            if (methodCallSymbol.IsDispatcherBeginInvoke())
            {
                Helper.WriteLogger(InterestingCallsFile, " //Dispatcher// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[0]++;
            }
            else if (methodCallSymbol.IsControlBeginInvoke())
            {
                Helper.WriteLogger(InterestingCallsFile, " //Form.Control// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[1]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsBeginInvoke()) // look at the synchronization context
            {
                Helper.WriteLogger(InterestingCallsFile, " //ThreadPool with Dispatcher// " + methodCall + " \\\\\\\\\\\r\n");
                NumPatternUsages[2]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem() && methodCall.ContainsSynchronizationContext()) // look at the synchronization context
            {
                Helper.WriteLogger(InterestingCallsFile, " //ThreadPool with Context// " + methodCall + " \\\\\\\\\\\r\n");
                NumPatternUsages[3]++;
            }
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
            {
                Helper.WriteLogger(InterestingCallsFile, " //ThreadPool// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[4]++;
            }
            else if (methodCallSymbol.IsBackgroundWorkerRunWorkerAsync())
            {
                Helper.WriteLogger(InterestingCallsFile, " //BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[5]++;
            }
            else if (methodCallSymbol.IsThreadStart())
            {
                Helper.WriteLogger(InterestingCallsFile, " //Thread// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[6]++;
            }
            else if (methodSymbolString.Contains("System.IAsyncResult") || (!methodCallSymbol.ReturnsVoid && methodCallSymbol.ReturnType.ToString().Contains("System.IAsyncResult")))
            {
                Helper.WriteLogger(InterestingCallsFile, " //APM// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[7]++;
            }
            else if (methodCallSymbol.ReturnsTask())
            {
                Helper.WriteLogger(InterestingCallsFile, " //TAP// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                NumPatternUsages[8]++;
            }
            else if (methodCall.CallsAsyncMethod() && methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First().IsEAPCompletedMethod())
            {
                Helper.WriteLogger(InterestingCallsFile, " //EAP// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                Helper.WriteLogger(@"C:\Users\david\Desktop\temp.txt", methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First() + "\\\\\\\\\\\r\n");
                NumPatternUsages[9]++;
            }
        }
    }

    internal class Walker : SyntaxWalker
    {
        private readonly AsyncAnalysis _outer;
        private bool _ui;

        public Walker(AsyncAnalysis outer)
        {
            _outer = outer;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !_ui)
            {
                _ui = true;
                _outer.NumUIClasses++;
            }

            base.VisitUsingDirective(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.ParameterList.Parameters.Any(param => param.Type.ToString().EndsWith("EventArgs")))
            {
                _outer.NumEventHandlerMethods++;
                _outer.ProcessMethodCallsInMethod(node, 0);
            }
            // detect async methods
            if (node.Modifiers.ToString().Contains("async"))
                _outer.NumAsyncMethods++;

            base.VisitMethodDeclaration(node);
        }

    }
}

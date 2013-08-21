using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;

namespace Analysis
{
    internal class AsyncAwaitDetectionWalker : SyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public List<String> AnalyzedMethods { get; set; }

        private static string[] BlockingMethodCalls = { "Thread.Sleep", "Task.WaitAll", "Task.WaitAny", "Task.Wait", ".Result" };

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasAsyncModifier())
            {
                if (node.ReturnType.ToString().Equals("void"))
                {
                    if (node.HasEventArgsParameter())
                        Result.asyncAwaitResults.NumAsyncVoidEventHandlerMethods++;
                    else
                        Result.asyncAwaitResults.NumAsyncVoidNonEventHandlerMethods++;
                }
                else
                    Result.asyncAwaitResults.NumAsyncTaskMethods++;


                if (!node.Body.ToString().Contains("await"))
                {
                    Result.asyncAwaitResults.NumAsyncMethodsNotHavingAwait++;
                    Logs.TempLog.Info("NOTHAVINGAWAIT\r\n{0}\r\n------------------------------", node);
                }

                int numAwaits = Regex.Matches(node.Body.ToString(), "await").Count;

                if (numAwaits > 6)
                    Logs.TempLog.Info("MANYAWAITS\r\n{0}\r\n------------------------------", node);

                if (node.Body.ToString().Contains("ConfigureAwait"))
                {
                    Result.asyncAwaitResults.NumAsyncMethodsHavingConfigureAwait++;
                    Logs.TempLog.Info("CONFIGUREAWAIT\r\n{0}\r\n------------------------------", node.ToString());
                }

                var blockings = BlockingMethodCalls.Where(a => node.Body.ToString().Contains(a));
                if (blockings.Count() > 0)
                {
                    Logs.TempLog.Info("BLOCKING\r\n{0}\r\n{1}\r\n------------------------------", blockings.First(), node);
                    Result.asyncAwaitResults.NumAsyncMethodsHavingBlockingCalls++;
                }
 
                ProcessMethodCallsInMethod(node, 0, node.Identifier.ToString() + node.ParameterList.ToString());
            }

            //if (node.HasEventArgsParameter())
            //{
            //    ProcessMethodCallsInMethod(node, 0, node.Identifier.ToString() + node.ParameterList.ToString());

            //}

            base.VisitMethodDeclaration(node);
        }

        public Enums.SyncDetected DetectSynchronousUsages(MethodSymbol methodCallSymbol)
        {
            var list = SemanticModel.LookupSymbols(0, container: methodCallSymbol.ContainingType,
                                includeReducedExtensionMethods: true);

            var name = methodCallSymbol.Name;
            Enums.SyncDetected type = Enums.SyncDetected.None;

            if (name.Equals("Invoke"))
                return type;

            foreach (var tmp in list)
            {
                if (tmp.Name.Equals("Begin" + name))
                {
                    type |= Enums.SyncDetected.APMReplacable;
                }
                if (tmp.Name.Equals(name + "Async"))
                {
                    type |= Enums.SyncDetected.TAPReplacable;
                }
            }
            ExpressionStatementSyntax a;
           
            return type;
        }

        private void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n, string topAncestor)
        {
            var hashcode = node.Identifier.ToString() + node.ParameterList.ToString();
            if (!AnalyzedMethods.Contains(hashcode))
            {
                AnalyzedMethods.Add(hashcode);

                var newMethods = new List<MethodDeclarationSyntax>();

                try
                {
                    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var semanticModelForThisMethodCall = Document.Project.Solution.GetDocument(methodCall.SyntaxTree).GetSemanticModelAsync().Result;

                        var methodCallSymbol = (MethodSymbol)semanticModelForThisMethodCall.GetSymbolInfo(methodCall).Symbol;

                        if (methodCallSymbol != null)
                        {
                            var synctype = DetectSynchronousUsages((MethodSymbol)methodCallSymbol.OriginalDefinition);

                            if (synctype != Utilities.Enums.SyncDetected.None)
                            {
                                Logs.TempLog.Info("{0} {1} {2} {3}\r\n{4} {5}\r\n\r\n{6}\r\n--------------------------", synctype, n, topAncestor, Document.FilePath, methodCallSymbol, methodCall, node);
                                Logs.TempLog2.Info("{0} {1}", methodCallSymbol.ContainingType, methodCallSymbol, synctype);
                            }

                            var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
                            if (methodDeclarationNode != null)
                                newMethods.Add(methodDeclarationNode);
                        }
                    }

                    foreach (var newMethod in newMethods)
                        ProcessMethodCallsInMethod(newMethod, n + 1, topAncestor);
                }
                catch (Exception ex)
                {
                    Logs.Log.Warn("Caught exception while processing method call node: {0} @ {1}", node, ex.Message);

                    if (!(
                          ex is FormatException ||
                          ex is ArgumentException ||
                          ex is PathTooLongException))
                        throw;
                }
            }
        }
    }
}
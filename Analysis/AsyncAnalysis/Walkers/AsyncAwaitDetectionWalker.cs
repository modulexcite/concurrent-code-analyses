using Microsoft.Build.Exceptions;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
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

        public IDocument Document { get; set; }

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
                    Logs.TempLog.Info(@"NOTHAVINGAWAIT {0} \r\n------------------------------", node);
                }

                int numAwaits = Regex.Matches(node.Body.ToString(), "await").Count;

                if (numAwaits > 6)
                    Logs.TempLog.Info("MANYAWAITS {0} \r\n------------------------------", node);

                if (node.Body.ToString().Contains("ConfigureAwait"))
                {
                    Result.asyncAwaitResults.NumAsyncMethodsHavingConfigureAwait++;
                    Logs.TempLog.Info(@"CONFIGUREAWAIT {0}  \r\n------------------------------", node.ToString());
                }
                if (BlockingMethodCalls.Any(a => node.Body.ToString().Contains(a)))
                {
                    Logs.TempLog.Info(@"BLOCKING {0} \r\n------------------------------", node);
                    Result.asyncAwaitResults.NumAsyncMethodsHavingBlockingCalls++;
                }
                ProcessMethodCallsInMethod(node, 0);
            }

            base.VisitMethodDeclaration(node);
        }

        public Enums.SyncDetected DetectSynchronousUsages(MethodSymbol methodCallSymbol)
        {
            var list = SemanticModel.LookupSymbols(0, methodCallSymbol.ContainingType,
                                                    options: LookupOptions.IncludeExtensionMethods);

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

            return type;
        }


        private void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
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
                        var methodCallSymbol = (MethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;

                        var synctype = DetectSynchronousUsages((MethodSymbol)methodCallSymbol.OriginalDefinition);

                        if (synctype != Utilities.Enums.SyncDetected.None)
                        {
                            Logs.TempLog.Info("{0} {1} {2}\r\n{3} {4} \r\n\r\n{5}\r\n --------------------------", synctype, n, Document.FilePath, methodCallSymbol, methodCall, node);
                            Logs.TempLog2.Info("{0} {1}", methodCallSymbol.ContainingType, methodCallSymbol, synctype);
                        }

                        var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
                        if (methodDeclarationNode != null)
                            newMethods.Add(methodDeclarationNode);
                    }

                    foreach (var newMethod in newMethods)
                        ProcessMethodCallsInMethod(newMethod, n + 1);
                }
                catch (Exception ex)
                {
                    Logs.Log.Warn("Caught exception while processing method call node: {0} @ {1}:{2}", node, Document.FilePath, node.Span.Start, ex);

                    if (!(ex is InvalidProjectFileException ||
                          ex is FormatException ||
                          ex is ArgumentException ||
                          ex is PathTooLongException))
                        throw;
                }
            }
        }
    }
}
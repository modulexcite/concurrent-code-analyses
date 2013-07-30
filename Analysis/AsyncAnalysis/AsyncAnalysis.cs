using Microsoft.Build.Exceptions;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            : base(dirName, appName)
        {
            result = new AsyncAnalysisResult(appName);
        }

        protected override bool FilterProject(Enums.ProjectType type)
        {
            if (type == Enums.ProjectType.WP7 || type == Enums.ProjectType.WP8)
            {
                //Result.WritePhoneProjects();
                return true;
            }
            return false;
        }

        protected override void VisitDocument(IDocument document, SyntaxNode root)
        {
            if (FilterDocument(document))
            {
                SyntaxWalker walker;

                //walker = new EventHandlerMethodsWalker()
                //{
                //    Analysis = this,
                //    Result = Result,
                //};

                walker = new InvocationsWalker()
                {
                    Analysis = this,
                    Result = Result,
                    SemanticModel = (SemanticModel)document.GetSemanticModel(),
                    Document = document,
                };
                walker.Visit(root);
            }
        }

        private bool FilterDocument(IDocument doc)
        {
            if (Path.GetDirectoryName(doc.FilePath).Contains(@"\Service References\"))
                return false;

            return true;
        }

        public void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            var newMethods = new List<MethodDeclarationSyntax>();
            Result.WriteNodeToCallTrace(node, n);

            var doc = CurrentSolution.GetDocument(node.SyntaxTree.GetRoot().SyntaxTree);

            try
            {
                var semanticModel = doc.GetSemanticModel();
                foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var methodCallSymbol = (MethodSymbol)((SemanticModel)semanticModel).GetSymbolInfo(methodCall).Symbol;

                    var type = DetectAsynchronousUsages(methodCall, methodCallSymbol);
                    Result.StoreDetectedAsyncUsage(type);
                    Result.WriteDetectedAsyncToCallTrace(type, methodCallSymbol);

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
                Logs.Log.Warn("Caught exception while processing method call node: {0} @ {1}:{2}", node, doc.FilePath, node.Span.Start, ex);

                if (!(ex is InvalidProjectFileException ||
                      ex is FormatException ||
                      ex is ArgumentException ||
                      ex is PathTooLongException))
                    throw;
            }
        }

        public Enums.AsyncDetected DetectAsynchronousUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            // DETECT ASYNC CALLS
            if (methodCallSymbol.IsThreadStart())
                return Enums.AsyncDetected.Thread;
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
                return Enums.AsyncDetected.Threadpool;
            else if (methodCallSymbol.IsAsyncDelegate())
                return Enums.AsyncDetected.AsyncDelegate;
            else if (methodCallSymbol.IsBackgroundWorkerMethod())
                return Enums.AsyncDetected.BackgroundWorker;
            else if (methodCallSymbol.IsTPLMethod())
                return Enums.AsyncDetected.TPL;
            // DETECT GUI UPDATE CALLS
            else if (methodCallSymbol.IsISynchronizeInvokeMethod())
                return Enums.AsyncDetected.ISynchronizeInvoke;
            else if (methodCallSymbol.IsControlBeginInvoke())
                return Enums.AsyncDetected.ControlInvoke;
            else if (methodCallSymbol.IsDispatcherBeginInvoke())
                return Enums.AsyncDetected.Dispatcher;
            // DETECT PATTERNS
            else if (methodCallSymbol.IsAPMBeginMethod())
                return Enums.AsyncDetected.APM;
            else if (methodCall.IsEAPMethod())
                return Enums.AsyncDetected.EAP;
            else if (methodCallSymbol.IsTAPMethod())
                return Enums.AsyncDetected.TAP;

            //
            else
                return Enums.AsyncDetected.None;
        }

        public Enums.SyncDetected DetectSynchronousUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var list = methodCallSymbol.ContainingType.MemberNames;

            var name = methodCallSymbol.Name;

            Enums.SyncDetected type = Enums.SyncDetected.None;

            foreach (var tmp in list)
            {
                if (tmp.ToString().Equals("Begin" + name))
                {
                    type |= Enums.SyncDetected.APMReplacable;
                }
                if (tmp.ToString().Equals(name + "Async"))
                {
                    type |= Enums.SyncDetected.TAPReplacable;
                    // TODO: look at return type of compilation.GetTypeByMetadataName(methodCallSymbol.ContainingType + "."+tmp) to check whether it is TAP or EAP
                }
            }

            return type;
        }



        public void APMDiagnosisDetection(MethodSymbol symbol, InvocationExpressionSyntax node, IDocument document, SemanticModel semanticModel)
        {
            
            if ((symbol.ToString().Contains("BeginAction") || symbol.ToString().Contains("System.Func") || symbol.ToString().Contains("System.Action")) && symbol.ToString().Contains("Invoke"))
            {
                Logs.TempLog.Info(@"FILTERED {0} {1} {2}", document.FilePath, node, symbol);
                return;
            }
            //PRINT ALL APM BEGIN METHODS
            Logs.APMDiagnosisLog.Info(@"Document: {0}", document.FilePath);
            Logs.APMDiagnosisLog.Info(@"Symbol: {0}", symbol);
            Logs.APMDiagnosisLog.Info(@"Invocation: {0}", node);
            Logs.APMDiagnosisLog.Info("---------------------------------------------------");

            Result.apmDiagnosisResults.NumAPMBeginMethods++;
            var statement = node.Ancestors().OfType<StatementSyntax>().First();
            var ancestors = node.Ancestors().OfType<MethodDeclarationSyntax>();
            if (ancestors.Any())
            {
                var method = ancestors.First();

                bool isFound = false;
                bool isAPMFollowed = false;
                foreach (var tmp in method.Body.ChildNodes())
                {
                    if (isFound)
                        isAPMFollowed = true;
                    if (statement == tmp)
                        isFound = true;
                }

                if (isAPMFollowed)
                {
                    Result.apmDiagnosisResults.NumAPMBeginFollowed++;
                    Logs.TempLog.Info(@"APMFOLLOWED {0}", method);
                }
            }

            int c = 0;
            foreach (var arg in symbol.Parameters)
            {
                if (arg.ToString().Contains("AsyncCallback"))
                    break;
                c++;
            }

            int i = 0;
            foreach (var arg in node.ArgumentList.Arguments)
            {
                if (c == i)
                {
                    Logs.APMDiagnosisLog2.Info("{0}: {1}", arg.Expression.Kind, arg);

                    if (arg.Expression.Kind.ToString().Contains("IdentifierName"))
                    {
                        var methodSymbol = semanticModel.GetSymbolInfo(arg.Expression).Symbol;

                        if (methodSymbol.Kind.ToString().Equals("Method"))
                        {
                            var methodDefinition = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxNodes.First();

                            if (methodDefinition.Body.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(a => a.Name.ToString().StartsWith("End")))
                            {
                                Logs.TempLog.Info(@"HEYOOO: {0} {1} \r\n Argument: {2} \r\n DeclaringSyntaxNodes: {3}", Document.FilePath, node, arg.Expression, methodDefinition);
                                Console.WriteLine("HEYOOO");
                            }
                            else
                            {
                                Logs.TempLog.Info(@"FUCKK: {0} {1} \r\n Argument: {2} \r\n DeclaringSyntaxNodes: {3}", Document.FilePath, node, arg.Expression, methodDefinition);
                                Console.WriteLine("FUCKK");
                            }
                        }
                    }

                    break;
                }
                i++;
            }
            //if (symbol.IsAPMEndMethod())
            //{
            //    Result.NumAPMEndMethods++;

            //    var ancestors= node.Ancestors().OfType<TryStatementSyntax>();
            //    if (ancestors.Any())
            //    {
            //        //TempLog.Info(@"TRYCATCHED ENDXXX {0}",  ancestors.First() );
            //        Result.NumAPMEndTryCatchedMethods++;
            //    }

            //    SyntaxNode block=null;
            //    var lambdas = node.Ancestors().OfType<SimpleLambdaExpressionSyntax>();
            //    if (lambdas.Any())
            //    {
            //        block = lambdas.First();
            //    }

            //    if (block == null)
            //    {
            //        var lambdas2 = node.Ancestors().OfType<ParenthesizedLambdaExpressionSyntax>();
            //        if (lambdas2.Any())
            //            block = lambdas2.First();
            //    }

            //    if (block == null)
            //    {
            //        var ancestors2 = node.Ancestors().OfType<MethodDeclarationSyntax>();
            //        if (ancestors2.Any())
            //            block = ancestors2.First();

            //    }

            //    if (block.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(a => a.Name.ToString().StartsWith("Begin") && !a.Name.ToString().Equals("BeginInvoke")))
            //    {
            //        //TempLog.Info(@"NESTED ENDXXX {0}", block);
            //        Result.NumAPMEndNestedMethods++;
            //    }

            //}
        }
    }
}
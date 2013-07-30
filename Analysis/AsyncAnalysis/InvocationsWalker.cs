using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using System.Configuration;
using System.Linq;
using Utilities;

namespace Analysis
{
    internal class InvocationsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis { get; set; }

        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public IDocument Document { get; set; }


        private bool uiClass;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (bool.Parse(ConfigurationManager.AppSettings["IsGeneralAsyncDetectionEnabled"]))
            {
                if (node.IsInSystemWindows() && !uiClass)
                {
                    uiClass = true;
                    Result.generalAsyncResults.NumUIClasses++;
                }
            }

            base.VisitUsingDirective(node);
        }

        public override void VisitClassDeclaration(Roslyn.Compilers.CSharp.ClassDeclarationSyntax node)
        {
            if ((node.BaseList != null) && (node.BaseList.ToString().Contains("ClientBase") || node.BaseList.ToString().Contains("ChannelBase")))
            {
                // IGNORE WCF SERVICES
            }
            else
                base.VisitClassDeclaration(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                if (bool.Parse(ConfigurationManager.AppSettings["IsAsyncUsageDetectionEnabled"]))
                {
                    var asynctype = Analysis.DetectAsynchronousUsages(node, symbol);
                    Result.StoreDetectedAsyncUsage(asynctype);
                    Result.WriteDetectedAsyncUsage(asynctype, Document.FilePath, symbol);
                }

                if (bool.Parse(ConfigurationManager.AppSettings["IsSyncUsageDetectionEnabled"]))
                {
                    var synctype = Analysis.DetectSynchronousUsages(node, (MethodSymbol)symbol.OriginalDefinition);
                    Result.StoreDetectedSyncUsage(synctype);
                    Result.WriteDetectedSyncUsage(synctype, Document.FilePath, (MethodSymbol)symbol.OriginalDefinition);
                    if (synctype != Utilities.Enums.SyncDetected.None
                            && node.Ancestors().OfType<MethodDeclarationSyntax>().Any(method => method.HasAsyncModifier()))
                    {
                        Result.syncUsageResults.NumGUIBlockingSyncUsages++;
                        Logs.TempLog.Info(@"GUIBLOCKING {0}", node.Ancestors().OfType<MethodDeclarationSyntax>().First().ToString());
                    }
                }

                if (bool.Parse(ConfigurationManager.AppSettings["IsAPMDiagnosisDetectionEnabled"]))
                {
                    if (symbol.IsAPMBeginMethod())
                    {
                        if ((symbol.ToString().Contains("BeginAction") || symbol.ToString().Contains("System.Func") || symbol.ToString().Contains("System.Action")) && symbol.ToString().Contains("Invoke"))
                        {
                            Logs.TempLog.Info(@"FILTERED {0} {1} {2}", Document.FilePath, node, symbol);
                            return;
                        }
                        //PRINT ALL APM BEGIN METHODS
                        Logs.APMDiagnosisLog.Info(@"Document: {0}", Document.FilePath);
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
                                    var methodSymbol = SemanticModel.GetSymbolInfo(arg.Expression).Symbol;

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

            base.VisitInvocationExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (bool.Parse(ConfigurationManager.AppSettings["IsAsyncAwaitDetectionEnabled"]))
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
                        Result.asyncAwaitResults.NumAsyncMethodsNotHavingAwait++;

                    if (node.Body.ToString().Contains("ConfigureAwait"))
                    {
                        Result.asyncAwaitResults.NumAsyncMethodsHavingConfigureAwait++;
                        Logs.TempLog.Info(@"CONFIGUREAWAIT {0}", node.ToString());
                    }
                    if (Constants.BlockingMethodCalls.Any(a => node.Body.ToString().Contains(a)))
                    {
                        Logs.TempLog.Info(@"BLOCKING {0}", node.ToString());
                        Result.asyncAwaitResults.NumAsyncMethodsHavingBlockingCalls++;
                    }
                }
            }



            if (node.HasEventArgsParameter())
            {
                Result.generalAsyncResults.NumEventHandlerMethods++;
                Analysis.ProcessMethodCallsInMethod(node, 0);
            }

            base.VisitMethodDeclaration(node);
        }
    }
}
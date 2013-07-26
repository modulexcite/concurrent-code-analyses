using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.Linq;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using Utilities;
using System.Configuration;

namespace Analysis
{
    internal class InvocationsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis { get; set; }
        public AsyncAnalysisResult Result { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public IDocument Document { get; set; }


        protected static readonly Logger TempLog = LogManager.GetLogger("TempLog");
        protected static readonly Logger APMDiagnosisLog = LogManager.GetLogger("APMDiagnosisLog");
        protected static readonly Logger APMDiagnosisLog2 = LogManager.GetLogger("APMDiagnosisLog2");
        private bool uiClass;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (bool.Parse(ConfigurationManager.AppSettings["IsUIClassDetectionEnabled"]))
            {
                if (node.IsInSystemWindows() && !uiClass)
                {
                    uiClass = true;
                    Result.NumUIClasses++;
                }
            }

            base.VisitUsingDirective(node);
        }


        public override void VisitClassDeclaration(Roslyn.Compilers.CSharp.ClassDeclarationSyntax node)
        {
            if ( (node.BaseList != null) && (node.BaseList.ToString().Contains("ClientBase") || node.BaseList.ToString().Contains("ChannelBase")))
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
                        Result.NumGUIBlockingSyncUsages++;
                        TempLog.Info(@"GUIBLOCKING {0}", node.Ancestors().OfType<MethodDeclarationSyntax>().First().ToString());

                    }
                }

                if (bool.Parse(ConfigurationManager.AppSettings["IsAPMDiagnosisDetectionEnabled"]))
                {

                    if (symbol.IsAPMBeginMethod())
                    {
                        //PRINT ALL APM BEGIN METHODS
                        APMDiagnosisLog.Info(@"Document: {0}", Document.FilePath);
                        APMDiagnosisLog.Info(@"Symbol: {0}", symbol);
                        APMDiagnosisLog.Info(@"Invocation: {0}", node);
                        APMDiagnosisLog.Info("---------------------------------------------------");

                        Result.NumAPMBeginMethods++;
                        var statement = node.Ancestors().OfType<StatementSyntax>().First();
                        var ancestors = node.Ancestors().OfType<MethodDeclarationSyntax>();
                        if (ancestors.Any())
                        {
                            var method = ancestors.First();

                            bool isFound=false;
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
                                Result.NumAPMBeginFollowed++;
                                TempLog.Info(@"APMFOLLOWED {0}", method);
                            }
                        }

                        int c= 0;
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
                                APMDiagnosisLog2.Info("{0}: {1}", arg.Expression.Kind, arg);

                                if (arg.Expression.Kind.ToString().Contains("IdentifierName"))
                                {
                                    var methodSymbol = SemanticModel.GetSymbolInfo(arg.Expression).Symbol;
                                  
                                    if (methodSymbol.Kind.ToString().Equals("Method"))
                                    { 
                                        var methodDefinition = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxNodes.First();

                                        if (methodDefinition.Body.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(a => a.Name.ToString().StartsWith("End")))
                                        {
                                            TempLog.Info(@"HEYOOO: {0} \r\n Argument: {1} \r\n DeclaringSyntaxNodes: {2}", node, arg.Expression, methodDefinition);
                                            Console.WriteLine("HEYOOO");
                                        }
                                        else
                                        {
                                            TempLog.Info(@"FUCKK: {0} \r\n Argument: {1} \r\n DeclaringSyntaxNodes: {2}", node, arg.Expression, methodDefinition);
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
                            Result.NumAsyncVoidEventHandlerMethods++;
                        else
                            Result.NumAsyncVoidNonEventHandlerMethods++;
                    }
                    else
                        Result.NumAsyncTaskMethods++;

                    if (!node.Body.ToString().Contains("await"))
                        Result.NumAsyncMethodsNotHavingAwait++;

                    if (node.Body.ToString().Contains("ConfigureAwait"))
                    {
                        Result.NumAsyncMethodsHavingConfigureAwait++;
                        TempLog.Info(@"CONFIGUREAWAIT {0}", node.ToString());
                    }
                    if (Constants.BlockingMethodCalls.Any(a => node.Body.ToString().Contains(a)))
                    {
                        TempLog.Info(@"BLOCKING {0}", node.ToString());
                        Result.NumAsyncMethodsHavingBlockingCalls++;
                    }

                }
            }

            base.VisitMethodDeclaration(node);
        }

    }
}
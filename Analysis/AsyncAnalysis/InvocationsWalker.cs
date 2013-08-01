using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
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
                // IGNORE WCF SERVICES WHICH ARE GENERATED AUTOMATICALLY
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
                        Analysis.APMDiagnosisDetection(symbol, node, Document, SemanticModel);
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


                    foreach (var invocationNode in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(invocationNode).Symbol;
                        if (symbol != null)
                        {
                            var synctype = Analysis.DetectSynchronousUsages(invocationNode, (MethodSymbol)symbol.OriginalDefinition);
                            if (synctype != Utilities.Enums.SyncDetected.None)
                            {
                                Logs.TempLog.Info("{0} - {1} {2} \r\n\r\n{3}\r\n --------------------------", Document.FilePath, synctype, invocationNode, node);
                            }
                        }
                    }

                    Logs.TempLog2.Info("{0}",Regex.Matches(node.Body.ToString(),"await").Count);

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

            if (bool.Parse(ConfigurationManager.AppSettings["IsGeneralAsyncDetectionEnabled"]))
            {
                if (node.HasEventArgsParameter())
                    Result.generalAsyncResults.NumEventHandlerMethods++;
            }
           

            base.VisitMethodDeclaration(node);
        }
    }
}
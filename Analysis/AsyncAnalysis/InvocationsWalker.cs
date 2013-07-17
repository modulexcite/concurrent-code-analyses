﻿using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.Linq;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using Utilities;

namespace Analysis
{
    internal class InvocationsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis { get; set; }
        public AsyncAnalysisResult Result { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public IDocument Document { get; set; }


        protected static readonly Logger TempLog = LogManager.GetLogger("TempLog");
        private bool uiClass;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !uiClass)
            {
                uiClass = true;
                Result.NumUIClasses++;
            }
            base.VisitUsingDirective(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                //var asynctype = Analysis.DetectAsynchronousUsages(node, symbol);

                //Result.StoreDetectedAsyncUsage(asynctype);

                //Result.WriteDetectedAsyncUsage(asynctype, Document.FilePath, symbol);


                var synctype = Analysis.DetectSynchronousUsages(node, (MethodSymbol)symbol.OriginalDefinition);

                Result.StoreDetectedSyncUsage(synctype);

                Result.WriteDetectedSyncUsage(synctype, Document.FilePath, (MethodSymbol)symbol.OriginalDefinition);

                if (synctype != Utilities.Enums.SyncDetected.None
                    && node.Ancestors().OfType<MethodDeclarationSyntax>().Any(method=> method.HasAsyncModifier()))
                {
                    Result.NumGUIBlockingSyncUsages++;
                    TempLog.Info(@"GUIBLOCKING {0}", node.Ancestors().OfType<MethodDeclarationSyntax>().First().ToString());
                    
                }
            }
            
                

            base.VisitInvocationExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
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
                    TempLog.Info(@"BLOCKING {0}",node.ToString());
                    Result.NumAsyncMethodsHavingBlockingCalls++;
                }
                
            }

            base.VisitMethodDeclaration(node);
        }

    }
}
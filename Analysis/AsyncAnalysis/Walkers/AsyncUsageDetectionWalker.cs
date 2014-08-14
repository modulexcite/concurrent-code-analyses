using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;

namespace Analysis
{
    internal class AsyncUsageDetectionWalker : CSharpSyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public bool IsEventHandlerWalkerEnabled { get; set; }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
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
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;
            
            if (symbol != null)
            {
                var asynctype = DetectAsynchronousUsages(node, symbol);
                Result.StoreDetectedAsyncUsage(asynctype);
                Result.WriteDetectedAsyncUsage(asynctype, Document.FilePath, symbol);
                Result.WriteDetectedAsyncUsageToTable(asynctype,Document,symbol,node);
                //if (asynctype == Enums.AsyncDetected.Task || asynctype == Enums.AsyncDetected.Threadpool || asynctype == Enums.AsyncDetected.Thread)
                //{
                //    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                //    {

                //        var methodCallSymbol = (MethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;

                //        if (methodCallSymbol != null)
                //        {
                //            var synctype = ((MethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

                //            if (synctype != Utilities.Enums.SyncDetected.None)
                //                Logs.TempLog.Info("LONGRUNNING {0} {1}\r\n{2}\r\n--------------------------", methodCallSymbol, Document.FilePath, node);
                //        }
                //    }
                //}
                 
            }


            base.VisitInvocationExpression(node);
        }


        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasAsyncModifier() && node.ToString().Contains("await"))
            {
                if(Result.CurrentAnalyzedProjectType == Enums.ProjectType.WP7)
                    Result.asyncUsageResults_WP7.NumAsyncAwaitMethods++;
                else
                    Result.asyncUsageResults_WP8.NumAsyncAwaitMethods++;
            }
                    

            base.VisitMethodDeclaration(node);
        }


        private Enums.AsyncDetected DetectAsynchronousUsages(InvocationExpressionSyntax methodCall, IMethodSymbol methodCallSymbol)
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
            else if (methodCallSymbol.IsTaskCreationMethod())
                return Enums.AsyncDetected.Task;


            //// DETECT GUI UPDATE CALLS
            //else if (methodCallSymbol.IsISynchronizeInvokeMethod())
            //    return Enums.AsyncDetected.ISynchronizeInvoke;
            //else if (methodCallSymbol.IsControlBeginInvoke())
            //    return Enums.AsyncDetected.ControlInvoke;
            //else if (methodCallSymbol.IsDispatcherBeginInvoke())
            //    return Enums.AsyncDetected.Dispatcher;


            // DETECT PATTERNS
            else if (methodCallSymbol.IsAPMBeginMethod())
                return Enums.AsyncDetected.APM;
            else if (methodCall.IsEAPMethod())
                return Enums.AsyncDetected.EAP;
            else if (methodCallSymbol.IsTAPMethod())
                return Enums.AsyncDetected.TAP;

            else
                return Enums.AsyncDetected.None;
        }


    }
}
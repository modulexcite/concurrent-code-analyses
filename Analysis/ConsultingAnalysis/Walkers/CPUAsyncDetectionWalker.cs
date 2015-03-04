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
    class CPUAsyncDetectionWalker : CSharpSyntaxWalker
    {
        public ConsultingAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public bool IsEventHandlerWalkerEnabled { get; set; }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if ((node.BaseList != null) && (node.BaseList.ToString().Contains("ClientBase") || node.BaseList.ToString().Contains("ChannelBase")))
            {
                // IGNORE WCF SERVICES WHICH ARE GENERATED AUTOMATICALLY
                Logs.ErrorLog.Info(@"{0} WCF is DETECTED", Result.SolutionPath);
            }
            else
                base.VisitClassDeclaration(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                Enums.AsyncDetected type = DetectCPUAsynchronousUsages(node,symbol);
                Result.StoreDetectedCPUAsyncUsage(type);

                // Check whether there are some blocking operations under task, threadpool, and thread!
                //if (type == Enums.AsyncDetected.Task || type == Enums.AsyncDetected.Threadpool || type == Enums.AsyncDetected.Thread)
                //{
                //    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                //    {
                //        var methodCallSymbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;

                //        if (methodCallSymbol != null)
                //        {
                //            var synctype = ((IMethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

                //            if (synctype != Utilities.Enums.SyncDetected.None)
                //                Logs.TempLog.Info("LONGRUNNING {0} {1}{2}--------------------------", methodCallSymbol, Document.FilePath, node.ToLog());
                //        }
                //    }
                //}

            }

            base.VisitInvocationExpression(node);
        }

        private Enums.AsyncDetected DetectCPUAsynchronousUsages(InvocationExpressionSyntax methodCall, IMethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            // DETECT CPU-BOUND ASYNC CALLS
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
            else if (methodCallSymbol.IsParallelForEach())
                return Enums.AsyncDetected.ParallelForEach;
            else if (methodCallSymbol.IsParallelFor())
                return Enums.AsyncDetected.ParallelFor;
            else if (methodCallSymbol.IsParallelInvoke())
                return Enums.AsyncDetected.ParallelInvoke;
            else
                return Enums.AsyncDetected.None;
        }
    }
}

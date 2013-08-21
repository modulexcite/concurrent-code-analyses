using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;

namespace Analysis
{
    internal class AsyncUsageDetectionWalker : SyntaxWalker
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
            var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;
            
            if (symbol != null)
            {
                var asynctype = DetectAsynchronousUsages(node, symbol);
                Result.StoreDetectedAsyncUsage(asynctype);
                Result.WriteDetectedAsyncUsage(asynctype, Document.FilePath, symbol);
                if (asynctype == Enums.AsyncDetected.APM)
                {
                    
                    if (Result.CurrentAnalyzedProjectType == Enums.ProjectType.WP7)
                        Result.asyncUsageResults.APMWP7++;
                    else
                        Result.asyncUsageResults.APMWP8++;
                }
                
            }


            base.VisitInvocationExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.ToString().Contains("PostAsync(string url, string postData, object user)"))
                Console.WriteLine(node);
            base.VisitMethodDeclaration(node);
        }

        private Enums.AsyncDetected DetectAsynchronousUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
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

        private void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            var newMethods = new List<MethodDeclarationSyntax>();
            Result.WriteNodeToCallTrace(node, n);

            try
            {
                foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var methodCallSymbol = (MethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;

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
                Logs.Log.Warn("Caught exception while processing method call node: {0} @ {1}:{2}", node, Document.FilePath, node.Span.Start, ex);

                if (!(
                      ex is FormatException ||
                      ex is ArgumentException ||
                      ex is PathTooLongException))
                    throw;
            }
        }
    }
}
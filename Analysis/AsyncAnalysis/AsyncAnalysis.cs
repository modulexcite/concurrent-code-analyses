using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System.IO;
using Microsoft.Build.Exceptions;
using Utilities;

namespace Analysis
{
    public class AsyncAnalysis : AnalysisBase
    {

        public enum Detected { APM = 0, EAP = 1, TAP = 2, Thread = 3, Threadpool = 4, AsyncDelegate = 5, BackgroundWorker = 6, TPL = 7, ISynchronizeInvoke = 8, ControlInvoke = 9, Dispatcher = 10, None };

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
            : base(dirName,appName)
        {
            result = new AsyncAnalysisResult(appName);
        }


        protected override void AnalyzeDocument(IDocument document)
        {
            var root = (SyntaxNode)  document.GetSyntaxTree().GetRoot();
            SemanticModel semanticModel= (SemanticModel) document.GetSemanticModel();
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
                SemanticModel = semanticModel,
                Document= document,
            };


            walker.Visit(root);
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

                    var type= DetectAsyncProgrammingUsages(methodCall, methodCallSymbol);
                    Result.StoreDetectedAsyncUsage(type);
                    Result.WriteDetectedAsyncToCallTrace(type, methodCallSymbol.ToString()); 

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
                Log.Warn("Caught exception while processing method call node: {0} @ {1}:{2}", node, doc.FilePath, node.Span.Start, ex);

                if (!(ex is InvalidProjectFileException ||
                      ex is FormatException ||
                      ex is ArgumentException ||
                      ex is PathTooLongException))
                    throw;
            }
        }

        public Detected DetectAsyncProgrammingUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            if (methodCallSymbol == null)
            {
                return Detected.None;
            }

            // DETECT PATTERNS
            if (methodCallSymbol.IsAPMBeginMethod())
                return Detected.APM;
            else if (methodCall.IsEAPMethod())
                return Detected.EAP;
            else if (methodCallSymbol.IsTAPMethod())
                return Detected.TAP;

            // DETECT ASYNC CALLS
            else if (methodCallSymbol.IsThreadStart())
                return Detected.Thread;
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
                return Detected.Threadpool;
            else if (methodCallSymbol.IsAsyncDelegate())
                return Detected.AsyncDelegate;
            else if (methodCallSymbol.IsBackgroundWorkerMethod())
                return Detected.BackgroundWorker;
            else if (methodCallSymbol.IsTPLMethod())
                return Detected.TPL;

            // DETECT GUI UPDATE CALLS
            else if (methodCallSymbol.IsISynchronizeInvokeMethod())
                return Detected.ISynchronizeInvoke;
            else if (methodCallSymbol.IsControlBeginInvoke())
                return Detected.ControlInvoke;
            else if (methodCallSymbol.IsDispatcherBeginInvoke())
                return Detected.Dispatcher;
            else
                return Detected.None;
        }
    }
}

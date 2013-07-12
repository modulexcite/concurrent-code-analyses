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


        public override bool FilterProject(Enums.ProjectType type)
        {
            if (type == Enums.ProjectType.WP7 || type == Enums.ProjectType.WP8)
            {
                //Result.WritePhoneProjects();
                return true;
            }
            return false;
        }

        protected override void VisitDocument(IDocument document,SyntaxNode root)
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
                SemanticModel = (SemanticModel) document.GetSemanticModel(),
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
                Log.Warn("Caught exception while processing method call node: {0} @ {1}:{2}", node, doc.FilePath, node.Span.Start, ex);

                if (!(ex is InvalidProjectFileException ||
                      ex is FormatException ||
                      ex is ArgumentException ||
                      ex is PathTooLongException))
                    throw;
            }
        }

        public Enums.Detected DetectAsyncProgrammingUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            if (methodCallSymbol == null)
            {
                return Enums.Detected.None;
            }

            // DETECT PATTERNS
            if (methodCallSymbol.IsAPMBeginMethod())
                return Enums.Detected.APM;
            else if (methodCall.IsEAPMethod())
                return Enums.Detected.EAP;
            else if (methodCallSymbol.IsTAPMethod())
                return Enums.Detected.TAP;

            // DETECT ASYNC CALLS
            else if (methodCallSymbol.IsThreadStart())
                return Enums.Detected.Thread;
            else if (methodCallSymbol.IsThreadPoolQueueUserWorkItem())
                return Enums.Detected.Threadpool;
            else if (methodCallSymbol.IsAsyncDelegate())
                return Enums.Detected.AsyncDelegate;
            else if (methodCallSymbol.IsBackgroundWorkerMethod())
                return Enums.Detected.BackgroundWorker;
            else if (methodCallSymbol.IsTPLMethod())
                return Enums.Detected.TPL;

            // DETECT GUI UPDATE CALLS
            else if (methodCallSymbol.IsISynchronizeInvokeMethod())
                return Enums.Detected.ISynchronizeInvoke;
            else if (methodCallSymbol.IsControlBeginInvoke())
                return Enums.Detected.ControlInvoke;
            else if (methodCallSymbol.IsDispatcherBeginInvoke())
                return Enums.Detected.Dispatcher;
            else
                return Enums.Detected.None;
        }
    }
}

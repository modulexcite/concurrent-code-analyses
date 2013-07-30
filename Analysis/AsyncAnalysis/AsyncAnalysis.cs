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
    }
}
using System;
using System.Linq;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Utilities;

namespace Analysis
{
    public class ThreadTaskAnalysis : AnalysisBase
    {
        static readonly string[] ThreadpoolSignatures =
            {
                "ThreadPool.QueueUserWorkItem", 
                "ThreadPool.RegisterWaitForSingleObject",
                "ThreadPool.UnsafeRegisterWaitForSingleObject",
                "ThreadPool.UnsafeQueueUserWorkItem", 
                "ThreadPool.UnsafeQueueNativeOverlapped"
            };

        private const string AppsFile = @"C:\Users\Semih\Desktop\apps.txt";

        private const string LoopTaskFile = @"C:\\Users\Semih\Desktop\loop.txt";
        private const string LoopTaskTestFile = @"C:\\Users\Semih\Desktop\loopTest.txt";
        private const string LoopThreadFile = @"C:\\Users\Semih\Desktop\loopThread.txt";
        private const string LoopThreadTestFile = @"C:\\Users\Semih\Desktop\loopThreadTest.txt";
        private const string InvokeFile = @"C:\\Users\Semih\Desktop\invoke.txt";
        private const string InvokeTestFile = @"C:\\Users\Semih\Desktop\invokeTest.txt";

        private const string ThreadFile = @"C:\\Users\Semih\Desktop\thread.txt";
        private const string ThreadTestFile = @"C:\\Users\Semih\Desktop\threadTest.txt";

        public int IsTpl;
        public int IsThreading;
        public int IsDataflow;
        public int NumberOfTaskInstances;
        public int NumberOfThreadInstances;

        public ThreadTaskAnalysis(string appName, string dirName)
            : base(appName, dirName)
        {
        }

        public override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();

            var loopWalker = new Walker()
            {
                Outer = this,
                Namespace = "System.Threading.Tasks",
                SemanticModel = document.GetSemanticModel(),
                Id = AppName + " " + document.Id
            };

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        public override void OnAnalysisCompleted()
        {

            Helper.WriteLogger(AppsFile, AppName + "," + IsTpl + "," + IsThreading + "," + IsDataflow + "," + NumberOfTaskInstances + "," + NumberOfThreadInstances + "," + (NumberOfThreadInstances + NumberOfTaskInstances) + "\n");
        }

        internal class Walker : SyntaxWalker
        {
            public ThreadTaskAnalysis Outer;
            public String Id;
            public ISemanticModel SemanticModel;
            public string Namespace;

            public override void VisitForStatement(ForStatementSyntax node)
            {
                IsCreateTaskInLoop(node);
                IsCreateThreadInLoop(node);
                base.VisitForStatement(node);
            }

            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                IsCreateTaskInLoop(node);
                IsCreateThreadInLoop(node);
                base.VisitForEachStatement(node);
            }

            public void IsCreateThreadInLoop(SyntaxNode node)
            {
                if (node.ToString().Contains("new Thread") && node.Parent.ToString().Contains(".Join"))
                {
                    Outer.NumberOfTaskInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(LoopThreadTestFile, Id, node.Parent.ToString());
                    else
                        Helper.WriteInstance(LoopThreadFile, Id, node.Parent.ToString());

                }
            }

            public void IsCreateTaskInLoop(SyntaxNode node)
            {
                if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(a => "StartNew".Equals(a.ToString()))
                    && !node.ToString().Contains("Stopwatch"))
                {
                    Outer.NumberOfTaskInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(LoopTaskTestFile, Id, node.Parent.ToString());
                    else
                        Helper.WriteInstance(LoopTaskFile, Id, node.Parent.ToString());
                }
            }

            public override void VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(a => "WaitAll".Equals(a.ToString()))
                    && node.Parent.ChildNodes().Any(a => a.ToString().Contains("StartNew"))
                    && !node.Parent.ChildNodes().Any(a => a is ForStatementSyntax)
                    && !node.Parent.ChildNodes().Any(a => a is ForEachStatementSyntax))
                {
                    Outer.NumberOfTaskInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(InvokeTestFile, Id, node.Parent.ToString());
                    else
                        Helper.WriteInstance(InvokeFile, Id, node.Parent.ToString());
                }

                base.VisitExpressionStatement(node);
            }

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                string name = node.Name.ToString();
                if ("System.Threading".Equals(name))
                    Outer.IsThreading++;
                if ("System.Threading.Tasks".Equals(name))
                    Outer.IsTpl++;
                if ("System.Threading.Tasks.Dataflow".Equals(name))
                    Outer.IsDataflow++;
                base.VisitUsingDirective(node);
            }


            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                //if (!Outer.isParallel)
                //    CheckWhetherMethodIsFromNamespace(node);

                var expression = node.Expression.ToString();

                if (ThreadpoolSignatures.Any(expression.Equals))
                {
                    Outer.NumberOfThreadInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(ThreadTestFile, Id, node.Parent.Parent.ToString());
                    else
                        Helper.WriteInstance(ThreadFile, Id, node.Parent.Parent.ToString());
                }
                base.VisitInvocationExpression(node);
            }

            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if ("Thread".Equals(node.Type.ToString()))
                {
                    Outer.NumberOfThreadInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(ThreadTestFile, Id, node.Parent.Parent.Parent.Parent.ToString());
                    else
                        Helper.WriteInstance(ThreadFile, Id, node.Parent.Parent.Parent.Parent.ToString());
                }
                base.VisitObjectCreationExpression(node);
            }

            //private void CheckWhetherMethodIsFromNamespace(ExpressionSyntax node)
            //{
            //    var isMatch = false;
            //    if (SemanticModel != null)
            //    {
            //        var symbolInfo = SemanticModel.GetSymbolInfo(node);
            //
            //        string ns = symbolInfo.Symbol.ContainingNamespace.ToDisplayString();
            //        if (ns == Namespace)
            //            isMatch = true;
            //    }
            //    Outer.isParallel = isMatch;
            //}
        }
    }

}

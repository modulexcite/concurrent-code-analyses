using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using System.IO;
using Microsoft;
using Microsoft.Build;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Evaluation;
using Utilities;

namespace Analysis
{
    public class ThreadTaskAnalysis : IAnalysis
    {
        static string[] threadpoolSignatures = { "ThreadPool.QueueUserWorkItem", "ThreadPool.RegisterWaitForSingleObject", "ThreadPool.UnsafeRegisterWaitForSingleObject", "ThreadPool.UnsafeQueueUserWorkItem", "ThreadPool.UnsafeQueueNativeOverlapped" };

        static string appsFile = @"C:\Users\Semih\Desktop\apps.txt";

        static string loopTaskFile = @"C:\\Users\Semih\Desktop\loop.txt";
        static string loopTaskTestFile = @"C:\\Users\Semih\Desktop\loopTest.txt";
        static string loopThreadFile = @"C:\\Users\Semih\Desktop\loopThread.txt";
        static string loopThreadTestFile = @"C:\\Users\Semih\Desktop\loopThreadTest.txt";
        static string invokeFile = @"C:\\Users\Semih\Desktop\invoke.txt";
        static string invokeTestFile = @"C:\\Users\Semih\Desktop\invokeTest.txt";
        
        static string threadFile = @"C:\\Users\Semih\Desktop\thread.txt";
        static string threadTestFile = @"C:\\Users\Semih\Desktop\threadTest.txt";


        public int isTPL;
        public int isThreading;
        public int isDataflow;
        public int numberOfTaskInstances;
        public int numberOfThreadInstances;





        public ThreadTaskAnalysis(string appName, string dirName) 
            : base(appName, dirName) 
        {
        }




        public override void analyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();

            var loopWalker = new Walker()
            {
                Outer= this,
                Namespace=  "System.Threading.Tasks",
                SemanticModel= document.GetSemanticModel(),
                Id = appName + " " + document.Id
            };

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        public override void onAnalysisCompleted()
        {

            Helper.WriteLogger(appsFile, appName + "," + isTPL + "," + isThreading + "," + isDataflow + "," + numberOfTaskInstances + "," + numberOfThreadInstances + "," + (numberOfThreadInstances + numberOfTaskInstances) + "\n");     
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
                    Outer.numberOfTaskInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(loopThreadTestFile, Id, node.Parent.ToString());
                    else
                        Helper.WriteInstance(loopThreadFile, Id, node.Parent.ToString());

                }
            }

            public void IsCreateTaskInLoop(SyntaxNode node)
            {
                if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(a => a.ToString().Equals("StartNew")) 
                    && !node.ToString().Contains("Stopwatch"))
                {
                    Outer.numberOfTaskInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(loopTaskTestFile, Id, node.Parent.ToString());
                    else
                        Helper.WriteInstance(loopTaskFile, Id, node.Parent.ToString());
                }
            }

            public override void VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if (node.DescendantNodes().OfType<IdentifierNameSyntax>().Any(a => a.ToString().Equals("WaitAll"))
                    && node.Parent.ChildNodes().Any(a => a.ToString().Contains("StartNew"))
                    && !node.Parent.ChildNodes().Any(a => a.GetType() == typeof(ForStatementSyntax))
                    && !node.Parent.ChildNodes().Any(a => a.GetType() == typeof(ForEachStatementSyntax)) )
                {
                    Outer.numberOfTaskInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(invokeTestFile, Id, node.Parent.ToString());
                    else
                        Helper.WriteInstance(invokeFile, Id, node.Parent.ToString());
                }

                base.VisitExpressionStatement(node);
            }
            
            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                string name = node.Name.ToString();
                if (name.Equals("System.Threading"))
                    Outer.isThreading++;
                if (name.ToString().Equals("System.Threading.Tasks"))
                    Outer.isTPL++;
                if (name.ToString().Equals("System.Threading.Tasks.Dataflow"))
                    Outer.isDataflow++;
 	            base.VisitUsingDirective(node);
            }


            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                //if (!Outer.isParallel)
                //    CheckWhetherMethodIsFromNamespace(node);

                String expression = node.Expression.ToString();



                if (threadpoolSignatures.Any(a => expression.Equals(a)))
                {
                    Outer.numberOfThreadInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(threadTestFile, Id, node.Parent.Parent.ToString());
                    else
                        Helper.WriteInstance(threadFile, Id, node.Parent.Parent.ToString());
                }
                base.VisitInvocationExpression(node);
            }



            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {

                if (node.Type.ToString().Equals("Thread"))
                {
                    Outer.numberOfThreadInstances++;
                    if (Id.ToLower().Contains("test"))
                        Helper.WriteInstance(threadTestFile, Id, node.Parent.Parent.Parent.Parent.ToString());
                    else
                        Helper.WriteInstance(threadFile, Id, node.Parent.Parent.Parent.Parent.ToString());
                }
                base.VisitObjectCreationExpression(node);
            }
            //private void CheckWhetherMethodIsFromNamespace(ExpressionSyntax node)
            //{
            //    var isMatch = false;
            //    if (SemanticModel != null)
            //    {
            //        var symbolInfo = SemanticModel.GetSymbolInfo(node);

            //        string ns = symbolInfo.Symbol.ContainingNamespace.ToDisplayString();
            //        if (ns == Namespace)
            //            isMatch = true;
            //    }
            //    Outer.isParallel = isMatch;
            //}
        }
    }



}

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
    public class AsyncAnalysis : IAnalysis
    {
        static string[] threadpoolSignatures = { "ThreadPool.QueueUserWorkItem", "ThreadPool.RegisterWaitForSingleObject", "ThreadPool.UnsafeRegisterWaitForSingleObject", "ThreadPool.UnsafeQueueUserWorkItem", "ThreadPool.UnsafeQueueNativeOverlapped" };

        static string appsFile = @"C:\Users\Semih\Desktop\UIApps.txt";
        static string eventHandlerMethodsFile = @"C:\Users\Semih\Desktop\eventHandlerMethods.txt";



        public int numUIClasses;





        public AsyncAnalysis(string appName, string dirName)
            : base(appName, dirName)
        {
        }




        public override void analyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();

            var loopWalker = new Walker()
            {
                Outer = this,
                Id = appName + " " + document.Id
            };

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        public override void onAnalysisCompleted()
        {
            //Helper.WriteLogger(appsFile, appName + "," + numUIClasses+ "\n");     
   
        }



        internal class Walker : SyntaxWalker
        {
            public AsyncAnalysis Outer;
            public String Id;


            public bool UI;

            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                string name = node.Name.ToString();
                if (name.StartsWith("System.Windows"))
                {
                    if (!UI)
                    {
                        UI = true;
                        Outer.numUIClasses++;
                    }
                }
                base.VisitUsingDirective(node);
            }

            

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                // parameter type should include routedeventargs

                if (node.ParameterList.Parameters.Any(param => param.Type.ToString().Equals("RoutedEventArgs")))
                    Helper.WriteInstance(eventHandlerMethodsFile, Id, node.ToString());
                
                base.VisitMethodDeclaration(node);
            }

        }
    }



}

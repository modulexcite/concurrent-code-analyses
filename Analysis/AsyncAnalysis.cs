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
        static string[] otherFingerprints = { "begininvoke", "async", "threadpool" };


        static string appsFile = @"C:\Users\Semih\Desktop\UIStatistics.txt";
        static string interestingCallsFile = @"C:\Users\Semih\Desktop\callsFromEventHandlers.txt";


        public int numUIClasses;
        public int numEventHandlerMethods;
        public int numAsyncMethods;
        public int[] numPatternUsages;



        public AsyncAnalysis(string appName, string dirName)
            : base(appName, dirName)
        {
            Helper.WriteLogger(interestingCallsFile, " #################\r\n" + appName + "\r\n#################\r\n");
            numPatternUsages = new int[11];
        }




        public override void AnalyzeDocument(IDocument document)
        {
            var syntaxTree = document.GetSyntaxTree();
            var loopWalker = new Walker()
            {
                Outer = this,
                Id = appName + " " + document.Id
            };

            loopWalker.Visit((SyntaxNode)syntaxTree.GetRoot());
        }

        public override void OnAnalysisCompleted()
        {
            Helper.WriteLogger(appsFile,
                appName + "," +
                numTotalProjects + "," +
                numUnloadedProjects + "," +
                numUnanalyzedProjects + "," +
                numAzureProjects + "," +
                numPhoneProjects + "," +
                numPhone8Projects + "," +
                numNet4Projects + "," +
                numNet45Projects + "," +
                numOtherNetProjects + ",");

            foreach (var pattern in numPatternUsages)
                Helper.WriteLogger(appsFile, pattern + ",");

            Helper.WriteLogger(appsFile, numAsyncMethods + ", " + numEventHandlerMethods + "," + numUIClasses + "\r\n");

        }



        public void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            List<MethodDeclarationSyntax> newMethods = new List<MethodDeclarationSyntax>();
            for (int i = 0; i < n; i++)
                Helper.WriteLogger(interestingCallsFile, " "); ;
            Helper.WriteLogger(interestingCallsFile, node.Identifier + " " + n + "\r\n");


            IDocument doc = currentSolution.GetDocument(node.SyntaxTree.GetRoot().SyntaxTree);

            try
            {
                var semanticModel = doc.GetSemanticModel();
                foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    MethodSymbol methodCallSymbol = (MethodSymbol)((SemanticModel)semanticModel).GetSymbolInfo(methodCall).Symbol;

                    DetectAsyncPatternUsages(methodCall, methodCallSymbol);

                    var methodDeclarationNode = FindMethodDeclarationNode(methodCallSymbol);

                    if (methodDeclarationNode != null && n < 5 && methodDeclarationNode != node)
                        newMethods.Add(methodDeclarationNode);
                }

                foreach (var newMethod in newMethods)
                    ProcessMethodCallsInMethod(newMethod, n + 1);

            }
            catch(Exception ex) 
            {
                if (ex is InvalidProjectFileException ||
                        ex is FormatException ||
                        ex is ArgumentException ||
                        ex is PathTooLongException)
                    return;
                else
                    throw;
            }



        }


        private MethodDeclarationSyntax FindMethodDeclarationNode(MethodSymbol methodCallSymbol)
        {
            if (methodCallSymbol == null)
                return null;

            var nodes = methodCallSymbol.DeclaringSyntaxNodes;

            if (nodes == null || nodes.Count == 0)
                return null;

            if (nodes.First() is MethodDeclarationSyntax)
                return (MethodDeclarationSyntax)nodes.First();

            return null;
            //var def = methodCallSymbol.FindSourceDefinition(currentSolution);

            //if (def != null && def.Locations != null && def.Locations.Count > 0)
            //{
            //    //methodCallSymbol.DeclaringSyntaxNodes.Firs
            //    var loc = def.Locations.First();
            //    var node = loc.SourceTree.GetRoot().FindToken(loc.SourceSpan.Start).Parent;
            //    if (node is MethodDeclarationSyntax)
            //        return (MethodDeclarationSyntax)node; 
            //}

        }

        public void DetectAsyncPatternUsages(InvocationExpressionSyntax methodCall, MethodSymbol methodCallSymbol)
        {

            string methodCallName = methodCall.Expression.ToString().ToLower();

            if (methodCallSymbol == null)
            {
                if (methodCallName.Contains("begininvoke") || methodCallName.Contains("async"))
                {
                    numPatternUsages[10]++;
                    Helper.WriteLogger(interestingCallsFile, " //Unresolved// " + methodCallName + " \\\\\\\\\\\r\n");
                }
                return;
            }

            var methodSymbolString = methodCallSymbol.ToString();

            if (methodSymbolString.Contains("Dispatcher.BeginInvoke")) // DispatcherOperation BeginInvoke(Delegate method,params Object[] args)
            {
                Helper.WriteLogger(interestingCallsFile, " //Dispatcher// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[0]++;
            }
            else if (methodSymbolString.Contains("Control.BeginInvoke"))  // a kind of APM public IAsyncResult BeginInvoke(Delegate method)
            {
                Helper.WriteLogger(interestingCallsFile, " //Form.Control// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[1]++;
            }
            else if (methodSymbolString.Contains("ThreadPool.QueueUserWorkItem") && methodCall.ToString().Contains("BeginInvoke")) // look at the synchronization context
            {
                Helper.WriteLogger(interestingCallsFile, " //ThreadPool with Dispatcher// " + methodCall + " \\\\\\\\\\\r\n");
                numPatternUsages[2]++;
            }
            else if (methodSymbolString.Contains("ThreadPool.QueueUserWorkItem") && methodCall.ToString().Contains("SynchronizationContext")) // look at the synchronization context
            {
                Helper.WriteLogger(interestingCallsFile, " //ThreadPool with Context// " + methodCall + " \\\\\\\\\\\r\n");
                numPatternUsages[3]++;
            }
            else if (methodSymbolString.Contains("ThreadPool.QueueUserWorkItem"))
            {
                Helper.WriteLogger(interestingCallsFile, " //ThreadPool// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[4]++;
            }
            else if (methodSymbolString.Contains("BackgroundWorker.RunWorkerAsync"))
            {
                Helper.WriteLogger(interestingCallsFile, " //BackgroundWorker// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[5]++;
            }
            else if (methodSymbolString.Contains("Thread.Start"))
            {
                Helper.WriteLogger(interestingCallsFile, " //Thread// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[6]++;
            }
            else if (methodSymbolString.Contains("System.IAsyncResult") || (!methodCallSymbol.ReturnsVoid && methodCallSymbol.ReturnType.ToString().Contains("System.IAsyncResult")))
            {
                Helper.WriteLogger(interestingCallsFile, " //APM// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[7]++;
            }
            else if (!methodCallSymbol.ReturnsVoid && methodCallSymbol.ReturnType.ToString().Contains("System.Threading.Tasks.Task"))
            {
                Helper.WriteLogger(interestingCallsFile, " //TAP// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                numPatternUsages[8]++;
            }
            else if (methodCallName.EndsWith("async") && methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First().ToString().Contains("Completed"))
            {
                Helper.WriteLogger(interestingCallsFile, " //EAP// " + methodCallSymbol + " \\\\\\\\\\\r\n");
                Helper.WriteLogger(@"C:\Users\Semih\Desktop\temp.txt", methodCall.Ancestors().OfType<MethodDeclarationSyntax>().First().ToString() + "\\\\\\\\\\\r\n");
                numPatternUsages[9]++;
            }
        }
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
            if (node.ParameterList.Parameters.Any(param => param.Type.ToString().EndsWith("EventArgs")))
            {
                Outer.numEventHandlerMethods++;
                Outer.ProcessMethodCallsInMethod(node, 0);
            }
            // detect async methods
            if (node.Modifiers.ToString().Contains("async"))
                Outer.numAsyncMethods++;

            base.VisitMethodDeclaration(node);
        }

    }






}

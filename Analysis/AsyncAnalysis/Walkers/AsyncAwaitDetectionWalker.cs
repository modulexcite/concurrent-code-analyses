using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;

namespace Analysis
{
    internal class AsyncAwaitDetectionWalker : SyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public List<String> AnalyzedMethods { get; set; }

        private static string[] BlockingMethodCalls = { "WaitAll", "WaitAny", "Wait", "Sleep"};

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {

            //if (node.HasAsyncModifier() && !node.ToString().Contains("await"))
            //{
            //    Logs.TempLog5.Info("NotHavingAwait {0}\r\n{1}\r\n------------------------------", Document.FilePath, node);
            //}


            if (node.HasAsyncModifier() && node.ToString().Contains("await"))
            {
                if (Result.CurrentAnalyzedProjectType == Enums.ProjectType.WP7)
                    Result.asyncAwaitResults.NumAsyncAwaitMethods_WP7++;
                else
                    Result.asyncAwaitResults.NumAsyncAwaitMethods_WP8++;


                if (node.ReturnType.ToString().Equals("void"))
                {
                    if (node.HasEventArgsParameter())
                        Result.asyncAwaitResults.NumAsyncVoidEventHandlerMethods++;
                    else
                        Result.asyncAwaitResults.NumAsyncVoidNonEventHandlerMethods++;
                }
                else
                    Result.asyncAwaitResults.NumAsyncTaskMethods++;


                //if (node.Body.ToString().Contains("ConfigureAwait"))
                //{

                //    Result.asyncAwaitResults.NumAsyncMethodsHavingConfigureAwait++;
                //    Logs.TempLog5.Info("ConfigureAwait {0}\r\n{1}\r\n------------------------------", Document.FilePath, node);
                //}



                //var symbol = SemanticModel.GetDeclaredSymbol(node);
                //bool isThereAnyCaller = false;
                //if (symbol != null)
                //{
                //    foreach (var refs in SymbolFinder.FindReferencesAsync(symbol, Document.Project.Solution).Result)
                //    {
                //        foreach (var locs in refs.Locations)
                //        {
                //            isThereAnyCaller = true;
                //            var caller = locs.Document.GetTextAsync().Result.Lines.ElementAt(locs.Location.GetLineSpan(false).StartLinePosition.Line).ToString();
                //            if (caller.Contains(".Result") || caller.Contains(".Wait"))
                //                Logs.TempLog5.Info("BlockingCaller {0}\r\n{1}\r\n{2}\r\n-----------------------", Document.FilePath, node, caller);

                //        }
                //    }
                //}



                //int numAwaits = Regex.Matches(node.Body.ToString(), "await").Count;
                //Logs.TempLog4.Info(numAwaits);
                //int numReturnAwaits = Regex.Matches(node.Body.ToString(), "return await").Count;

                //if (!node.ReturnType.ToString().Equals("void") &&
                //    !node.DescendantNodes().OfType<StatementSyntax>().Where(a => a.ToString().Contains("await")).Any(a => a.Ancestors().OfType<TryStatementSyntax>().Any()))
                //{
                //    if (numAwaits == numReturnAwaits)
                //        Logs.TempLog.Info("UnnecessaryAwaits {0}\r\n{1}\r\n-----------------------", Document.FilePath, node);
                //    else if (numAwaits == 1 && node.Body.Statements.Count > 0 && node.Body.Statements.Last().ToString().StartsWith("await"))
                //        Logs.TempLog.Info("UnnecessaryAwaits {0}\r\n{1}\r\n-----------------------", Document.FilePath, node);
                //}



                //foreach (var loop in node.DescendantNodes().Where(a => a is ForEachStatementSyntax || a is ForStatementSyntax || a is WhileStatementSyntax))
                //{
                //    foreach (var tap in loop.DescendantNodes().OfType<InvocationExpressionSyntax>())
                //    {
                //        MethodSymbol sym = (MethodSymbol)SemanticModel.GetSymbolInfo(tap).Symbol;
                //        if (sym != null && sym.IsTAPMethod())
                //            Logs.TempLog2.Info("ExpensiveAwaits {0} {1}\r\n{2}\r\n-----------------------", sym, Document.FilePath, node);
                //    }
                //}
                    

                //ProcessMethodCallsInMethod(node, 0, node.Identifier.ToString() + node.ParameterList.ToString());


                if (CheckConfigureAwaitUse(node,0))
                    Logs.TempLog.Info("ConfigureAwaitUse {0}\r\n{1}\r\n------------------------------",Document.FilePath,node);
                


            }

            //if (node.HasEventArgsParameter())
            //{
            //    var arg = node.ParameterList.Parameters.Where(param => param.Type.ToString().EndsWith("EventArgs")).First();

            //    var type = SemanticModel.GetTypeInfo(arg.Type).Type;

            //    if (type.ToString().StartsWith("System.Windows"))
            //    {
            //        ProcessMethodCallsInMethod(node, 0, node.Identifier.ToString() + node.ParameterList.ToString());
            //    }
            //}
            base.VisitMethodDeclaration(node);
        }

        private bool CheckConfigureAwaitUse(MethodDeclarationSyntax node, int n)
        {

            if (CheckUIElementAccess(node))
                return false;
            else
            {
                bool result=true;
                {
                    var newMethods = new List<MethodDeclarationSyntax>();
                    try
                    {
                        var semanticModel = Document.Project.Solution.GetDocument(node.SyntaxTree).GetSemanticModelAsync().Result;
                        if (semanticModel != null)
                        {
                            foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                            {
                                var methodCallSymbol = (MethodSymbol)semanticModel.GetSymbolInfo(methodCall).Symbol;

                                if (methodCallSymbol != null)
                                {
                                    var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
                                    if (methodDeclarationNode != null && n<10)
                                        newMethods.Add(methodDeclarationNode);
                                }
                            }
                        }
                        foreach (var newMethod in newMethods)
                            result = result && CheckConfigureAwaitUse(newMethod, n+1);
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }
                return result;
            }
        }

        private bool CheckUIElementAccess(MethodDeclarationSyntax node)
        {


            foreach (var identifier in node.Body.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;

                if (symbol != null)
                {
                    if (symbol.ToString().StartsWith("System.Windows.") || symbol.ToString().StartsWith("Microsoft.Phone."))
                        return true;
                }
            
            
            }
            return false;
        }


        private void ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n, string topAncestor)
        {
            var hashcode = node.Identifier.ToString() + node.ParameterList.ToString();

            bool asyncFlag = false;
            if (node.HasAsyncModifier())
                asyncFlag = true;

            if (!AnalyzedMethods.Contains(hashcode))
            {
                AnalyzedMethods.Add(hashcode);

                var newMethods = new List<MethodDeclarationSyntax>();
                try
                {
                    var semanticModel = Document.Project.Solution.GetDocument(node.SyntaxTree).GetSemanticModelAsync().Result;

                    foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => BlockingMethodCalls.Any(b => b.Equals(a.Name.ToString()))))
                    {
                        Logs.TempLog2.Info("BLOCKING {0} {1} {2}\r\n{3} \r\n{4}\r\n{5}\r\n--------------------------",asyncFlag, n, blocking, Document.FilePath, topAncestor, node);
                    }

                    foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => a.Name.ToString().Equals("Result")))
                    {
                        var s = semanticModel.GetSymbolInfo(blocking).Symbol;
                        if(s!=null && s.ToString().Contains("System.Threading.Tasks"))
                             Logs.TempLog2.Info("BLOCKING {0} {1} {2}\r\n{3} \r\n{4}\r\n{5}\r\n--------------------------",asyncFlag, n, blocking, Document.FilePath, topAncestor, node);
                    }

                    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var methodCallSymbol = (MethodSymbol)semanticModel.GetSymbolInfo(methodCall).Symbol;

                        if (methodCallSymbol != null)
                        {
                            var synctype = ((MethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

                            if (synctype != Utilities.Enums.SyncDetected.None)
                            {
                                if (!methodCallSymbol.Name.ToString().Equals("Invoke"))
                                    Logs.TempLog2.Info("LONGRUNNING {0} {1} {2} {3}\r\n{4} {5}\r\n{6}\r\n--------------------------",asyncFlag, n, methodCallSymbol, Document.FilePath, synctype, topAncestor, node);
                                Logs.TempLog3.Info("{0} {1}", methodCallSymbol.ContainingType, methodCallSymbol, synctype);
                            }

                            var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
                            if (methodDeclarationNode != null)
                                newMethods.Add(methodDeclarationNode);
                        }
                    }

                    foreach (var newMethod in newMethods)
                        ProcessMethodCallsInMethod(newMethod, n + 1, topAncestor);
                }
                catch (Exception ex)
                {
                    Logs.Log.Warn("Caught exception while processing method call node: {0} @ {1}", node, ex.Message);

                    if (!(
                          ex is FormatException ||
                          ex is ArgumentException ||
                          ex is PathTooLongException))
                        throw;
                }
            }
        }
    }
}
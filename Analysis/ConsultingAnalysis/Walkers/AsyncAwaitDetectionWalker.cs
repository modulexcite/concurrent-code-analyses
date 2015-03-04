using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Analysis
{
    internal class AsyncAwaitConsultingDetectionWalker : CSharpSyntaxWalker
    {
        public ConsultingAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }
        public List<String> AnalyzedMethods { get; set; }
        private static string[] BlockingMethodCalls = { "WaitAll", "WaitAny", "Wait", "Sleep" };

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
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;
            
            if (symbol != null)
            {
                var type = DetectIOAsynchronousUsages(node, symbol);
                Result.StoreDetectedIOAsyncUsage(type);

                //DetectBlockingOperations(node, symbol);

                //if (type == Enums.AsyncDetected.EAP)
                //    Logs.TempLog2.Info("{0}{1}*****************************************", Document.FilePath, node.FirstAncestorOrSelf<MethodDeclarationSyntax>().ToLog());

                if (type == Enums.AsyncDetected.TAP)
                    //Logs.TempLog3.Info("{0}{1}*****************************************", Document.FilePath, node.FirstAncestorOrSelf<MethodDeclarationSyntax>().ToLog());
                    Logs.TempLog3.Info("{0}", symbol);

                //if (type == Enums.AsyncDetected.APM)
                //    Logs.TempLog4.Info("{0}{1}*****************************************", Document.FilePath, node.FirstAncestorOrSelf<MethodDeclarationSyntax>().ToLog());

            }

            base.VisitInvocationExpression(node);
        }


        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!node.HasAsyncModifier())
            {
                base.VisitMethodDeclaration(node);
                return;
            }

            if (node.ToString().Contains("await"))
            {
                Result.asyncAwaitResults.NumAsyncMethods++;

                Logs.TempLog.Info(@"{0}{1}**********************************************", Document.FilePath, node.ToLog());

                if (!node.ReturnType.ToString().Equals("void"))
                {
                    Result.asyncAwaitResults.NumAsyncTaskMethods++;
                }

                if (IsFireForget(node))
                    Result.StoreDetectedAsyncMisuse(1, Document, node);

                if (IsUnnecessaryAsyncAwait(node))
                    Result.StoreDetectedAsyncMisuse(2, Document, node);

                if (IsThereLongRunning(node))
                    Result.StoreDetectedAsyncMisuse(3, Document, node);

                if (IsUnnecessarilyCaptureContext(node, 0))
                    Result.StoreDetectedAsyncMisuse(4, Document, node);


                //var symbol = SemanticModel.GetDeclaredSymbol(node);
                //if (symbol != null)
                //{

                //    foreach (var refs in SymbolFinder.FindReferencesAsync(symbol, Document.Project.Solution).Result)
                //    {
                //        foreach (var loc in refs.Locations)
                //        {
                //            var caller = loc.Document.GetTextAsync().Result.Lines.ElementAt(loc.Location.GetLineSpan().StartLinePosition.Line).ToString();
                //            if (caller.Contains(".Result") || caller.Contains(".Wait"))
                //                Logs.TempLog5.Info("{0}{1}{2}{3}************************************",
                //                    System.Environment.NewLine + "Blocking Caller: '" + caller.Trim() + "' from this file: " + loc.Document.FilePath + System.Environment.NewLine,
                //                    System.Environment.NewLine + "Async method Callee: " + System.Environment.NewLine,
                //                    node.ToLog(),
                //                    "From this file: " + Document.FilePath + System.Environment.NewLine + System.Environment.NewLine);
                //        }
                //    }
                //}
            }
            else
                Logs.AsyncMisuse.Info(@"{0} - {1}{2}**********************************************", Document.FilePath, "No Await", node.ToLog());

            base.VisitMethodDeclaration(node);
        }

        private void DetectBlockingOperations(InvocationExpressionSyntax methodCall, IMethodSymbol methodCallSymbol)
        {

            var methodDeclaration = methodCall.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var replacement = ((IMethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

            if (methodDeclaration != null)
            {
                if (replacement != "None")
                {
                    Logs.TempLog6.Info(@"{0}{1}{2}{3}**********************************************",
                                        Document.FilePath,
                                        System.Environment.NewLine + System.Environment.NewLine + "BLOCKING METHODCALL: " + methodCallSymbol.ToString(),
                                        System.Environment.NewLine + System.Environment.NewLine + "REPLACE IT WITH: " + replacement,
                                        methodDeclaration.ToLog());
                }
            }
        }

        private Enums.AsyncDetected DetectIOAsynchronousUsages(InvocationExpressionSyntax methodCall, IMethodSymbol methodCallSymbol)
        {
            var methodCallName = methodCall.Expression.ToString().ToLower();

            // DETECT PATTERNS
            if (methodCallSymbol.IsAPMBeginMethod())
                return Enums.AsyncDetected.APM;
            else if (methodCall.IsEAPMethod())
                return Enums.AsyncDetected.EAP;
            else if (methodCallSymbol.IsTAPMethod())
                return Enums.AsyncDetected.TAP;
            else
                return Enums.AsyncDetected.None;
        }


        private bool IsFireForget(MethodDeclarationSyntax node)
        {
            return node.ReturnType.ToString().Equals("void") && !node.HasEventArgsParameter();
        }

        private bool IsUnnecessaryAsyncAwait(MethodDeclarationSyntax node)
        {
            int numAwaits = Regex.Matches(node.Body.ToString(), "await").Count;
            int numReturnAwaits = Regex.Matches(node.Body.ToString(), "return await").Count;

            if (!node.ReturnType.ToString().Equals("void") &&
                !node.DescendantNodes().OfType<StatementSyntax>().Where(a => a.ToString().Contains("await")).Any(a => a.Ancestors().OfType<TryStatementSyntax>().Any()))
            {
                if (numAwaits == numReturnAwaits)
                    return true;
                else if (numAwaits == 1 && node.Body.Statements.Count > 0 && node.Body.Statements.Last().ToString().StartsWith("await"))
                    return true;
            }
            return false;
        }


        private bool IsThereLongRunning(MethodDeclarationSyntax node)
        {
            foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => BlockingMethodCalls.Any(b => b.Equals(a.Name.ToString()))))
                return true;

            foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var methodCallSymbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(methodCall).Symbol;

                if (methodCallSymbol != null)
                {
                    var replacement = ((IMethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

                    if (replacement != "None")
                    {
                        if (!methodCallSymbol.Name.ToString().Equals("Invoke"))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool IsUnnecessarilyCaptureContext(MethodDeclarationSyntax node, int n)
        {
            if (CheckUIElementAccess(node))
                return false;
            else
            {
                bool result = true;
                {
                    var newMethods = new List<MethodDeclarationSyntax>();
                    try
                    {
                        var semanticModel = Document.Project.Solution.GetDocument(node.SyntaxTree).GetSemanticModelAsync().Result;
                        if (semanticModel != null)
                        {
                            foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                            {
                                var methodCallSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(methodCall).Symbol;

                                if (methodCallSymbol != null)
                                {
                                    var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
                                    if (methodDeclarationNode != null && n < 10)
                                        newMethods.Add(methodDeclarationNode);
                                }
                            }
                        }
                        foreach (var newMethod in newMethods)
                            result = result && IsUnnecessarilyCaptureContext(newMethod, n + 1);
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
                        Logs.TempLog2.Info("BLOCKING {0} {1} {2}\r\n{3} \r\n{4}\r\n{5}\r\n--------------------------", asyncFlag, n, blocking, Document.FilePath, topAncestor, node);
                    }

                    foreach (var blocking in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Where(a => a.Name.ToString().Equals("Result")))
                    {
                        var s = semanticModel.GetSymbolInfo(blocking).Symbol;
                        if (s != null && s.ToString().Contains("System.Threading.Tasks"))
                            Logs.TempLog2.Info("BLOCKING {0} {1} {2}\r\n{3} \r\n{4}\r\n{5}\r\n--------------------------", asyncFlag, n, blocking, Document.FilePath, topAncestor, node);
                    }

                    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var methodCallSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(methodCall).Symbol;

                        if (methodCallSymbol != null)
                        {
                            var replacement = ((IMethodSymbol)methodCallSymbol.OriginalDefinition).DetectSynchronousUsages(SemanticModel);

                            if (replacement != "None")
                            {
                                if (!methodCallSymbol.Name.ToString().Equals("Invoke"))
                                    Logs.TempLog2.Info("LONGRUNNING {0} {1} {2} {3}\r\n{4} {5}\r\n{6}\r\n--------------------------", asyncFlag, n, methodCallSymbol, Document.FilePath, replacement, topAncestor, node);
                                Logs.TempLog3.Info("{0} {1}", methodCallSymbol.ContainingType, methodCallSymbol, replacement);
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
                    Logs.Console.Warn("Caught exception while processing method call node: {0} @ {1}", node, ex.Message);

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
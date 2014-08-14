using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using Utilities;

namespace Analysis
{
    internal class APMDiagnosisDetectionWalker : CSharpSyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                if (symbol.IsAPMBeginMethod())
                    APMDiagnosisDetection(symbol, node);

                if (symbol.IsAPMEndMethod())
                {
                    Result.apmDiagnosisResults.NumAPMEndMethods++;


                    var tmp = node.ArgumentList.Arguments.First().DescendantNodes().OfType<IdentifierNameSyntax>(); 
                    
                    if(tmp.Any())
                    {
                        var id = tmp.First();

                        if(node.Ancestors().OfType<BlockSyntax>().First().DescendantNodes().OfType<IdentifierNameSyntax>().Where(a=> (a.Identifier.ToString()== id.Identifier.ToString())).Count()>2)
                            Logs.TempLog3.Info("IAsyncResultSomewhereElse {0}\r\n{1}\r\n--------------------", Document.FilePath, node.Ancestors().OfType<BlockSyntax>().First());

                    }
                    

                   


                    //var ancestors2 = node.Ancestors().OfType<TryStatementSyntax>();
                    //if (ancestors2.Any())
                    //{
                    //    Logs.TempLog4.Info("TRYCATCHED ENDXXX:\r\n{0}\r\n---------------------------------------------------", ancestors2.First());
                    //    Result.apmDiagnosisResults.NumAPMEndTryCatchedMethods++;
                    //}

                    //SyntaxNode block = null;
                    //var lambdas = node.Ancestors().OfType<SimpleLambdaExpressionSyntax>();
                    //if (lambdas.Any())
                    //{
                    //    block = lambdas.First();
                    //}

                    //if (block == null)
                    //{
                    //    var lambdas2 = node.Ancestors().OfType<ParenthesizedLambdaExpressionSyntax>();
                    //    if (lambdas2.Any())
                    //        block = lambdas2.First();
                    //}

                    //if (block == null)
                    //{
                    //    var ancestors3 = node.Ancestors().OfType<MethodDeclarationSyntax>();
                    //    if (ancestors3.Any())
                    //        block = ancestors3.First();

                    //}

                    //if (block != null)
                    //{
                    //    if (block.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(a => a.Name.ToString().StartsWith("Begin") && !a.Name.ToString().Equals("BeginInvoke")))
                    //    {
                    //        Logs.TempLog5.Info("NESTED ENDXXX:\r\n{0}\r\n---------------------------------------------------", block);
                    //        Result.apmDiagnosisResults.NumAPMEndNestedMethods++;
                    //    }
                    //}

                }
            }

            base.VisitInvocationExpression(node);
        }



        private void APMDiagnosisDetection(IMethodSymbol symbol, InvocationExpressionSyntax node)
        {
            int c = GetIndexCallbackArgument(symbol);

            var callbackArg = node.ArgumentList.Arguments.ElementAt(c);

            if (callbackArg.Expression.CSharpKind().ToString().Contains("IdentifierName"))
                Logs.TempLog.Info("{0} {1}", callbackArg.Expression.CSharpKind(), SemanticModel.GetSymbolInfo(callbackArg.Expression).Symbol.Kind);
            else
                Logs.TempLog.Info("{0}", callbackArg.Expression.CSharpKind());
            
           
            //PRINT ALL APM BEGIN METHODS
            Logs.APMDiagnosisLog.Info(@"Document: {0}", Document.FilePath);
            Logs.APMDiagnosisLog.Info(@"Symbol: {0}", symbol);
            Logs.APMDiagnosisLog.Info(@"Invocation: {0}", node);
            Logs.APMDiagnosisLog.Info(@"CallbackType: {0}", callbackArg.Expression.CSharpKind());
            if (callbackArg.Expression.CSharpKind().ToString().Contains("IdentifierName"))
                Logs.APMDiagnosisLog.Info("{0}", SemanticModel.GetSymbolInfo(callbackArg.Expression).Symbol.Kind);
            Logs.APMDiagnosisLog.Info("---------------------------------------------------");

            Result.apmDiagnosisResults.NumAPMBeginMethods++;





            if (node.Ancestors().Where(a => (a is BinaryExpressionSyntax) || (a is VariableDeclaratorSyntax) || (a is VariableDeclarationSyntax) || (a is ReturnStatementSyntax)).Any() )
            {
                Logs.TempLog2.Info("UnderStatement {0}\r\n{1}\r\n--------------------", Document.FilePath, node.Ancestors().OfType<BlockSyntax>().First());
            }



            //var statement = node.Ancestors().OfType<StatementSyntax>().First();
            //var ancestors = node.Ancestors().OfType<MethodDeclarationSyntax>();
            //if (ancestors.Any())
            //{
            //    var method = ancestors.First();

            //    bool isFound = false;
            //    bool isAPMFollowed = false;
            //    foreach (var tmp in method.Body.ChildNodes())
            //    {
            //        if (isFound)
            //            isAPMFollowed = true;
            //        if (statement == tmp)
            //            isFound = true;
            //    }

            //    if (isAPMFollowed)
            //    {
            //        Result.apmDiagnosisResults.NumAPMBeginFollowed++;
            //        Logs.TempLog2.Info("APMFOLLOWED:\r\n{0}\r\n---------------------------------------------------", method);
                    
            //    }
            //}

            //SyntaxNode callbackBody = null;

            //if (callbackArg.Expression.Kind.ToString().Contains("IdentifierName"))
            //{
            //    var methodSymbol = SemanticModel.GetSymbolInfo(callbackArg.Expression).Symbol;

            //    if (methodSymbol.Kind.ToString().Equals("Method"))
            //        callbackBody = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            //}
            //else if (callbackArg.Expression.Kind.ToString().Contains("LambdaExpression"))
            //    callbackBody = node;

            //if (callbackBody != null)
            //{
            //    if (callbackBody.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(a => a.Name.ToString().StartsWith("End")))
            //        Logs.TempLog3.Info("APMEND in Callback:\r\n{0}\r\nCaller: {1}\r\nDeclaringSyntaxNodes:\r\n{2}\r\n--------------------------------------------------", Document.FilePath, node, callbackBody);
            //    else
            //        Logs.TempLog3.Info("NO APMEND in Callback:\r\n{0}\r\nCaller: {1}\r\nDeclaringSyntaxNodes:\r\n{2}\r\n--------------------------------------------------", Document.FilePath, node, callbackBody);
            //}


            
        }


        private int GetIndexCallbackArgument(IMethodSymbol symbol)
        {
            int c = 0;
            foreach (var arg in symbol.Parameters)
            {
                if (arg.ToString().Contains("AsyncCallback"))
                    break;
                c++;
            }
            return c;
        }
    }
}
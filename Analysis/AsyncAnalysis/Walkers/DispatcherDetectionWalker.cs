using Microsoft.Build.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Utilities;


namespace Analysis
{
    internal class DispatcherDetectionWalker : CSharpSyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public Dictionary<String, int> AnalyzedMethods { get; set; }


        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null && (symbol.IsDispatcherBeginInvoke()||  symbol.IsDispatcherInvoke()))
            {
                Logs.TempLog.Info("{0}\r\n-----------------------",node);

            }
            base.VisitInvocationExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasEventArgsParameter())
            {
                var arg= node.ParameterList.Parameters.Where(param => param.Type.ToString().EndsWith("EventArgs")).First();

                var type = SemanticModel.GetTypeInfo(arg.Type).Type;
                
                if (type.ToString().StartsWith("System.Windows"))
                {
                    int result = ProcessMethodCallsInMethod(node,0);
                    Logs.TempLog2.Info("{0}\r\n{1}\r\n--------------", result, node);

                }
            }

            base.VisitMethodDeclaration(node);
        }


        private int ProcessMethodCallsInMethod(MethodDeclarationSyntax node, int n)
        {
            int sum = 0; 
            var hashcode = node.Identifier.ToString() + node.ParameterList.ToString();
            if (!AnalyzedMethods.ContainsKey(hashcode))
            {
                var newMethods = new List<MethodDeclarationSyntax>();
                AnalyzedMethods.Add(hashcode, sum);
                try
                {
                    foreach (var methodCall in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var semanticModelForThisMethodCall = Document.Project.Solution.GetDocument(methodCall.SyntaxTree).GetSemanticModelAsync().Result;

                        var methodCallSymbol = (IMethodSymbol)semanticModelForThisMethodCall.GetSymbolInfo(methodCall).Symbol;

                        if (methodCallSymbol != null)
                        {

                            if (methodCallSymbol.IsDispatcherBeginInvoke() || methodCallSymbol.IsDispatcherInvoke())
                                AnalyzedMethods[hashcode]++;

                            var methodDeclarationNode = methodCallSymbol.FindMethodDeclarationNode();
                            if (methodDeclarationNode != null)
                                newMethods.Add(methodDeclarationNode);
                        }
                    }

                    if (n < 3)
                    {
                        foreach (var newMethod in newMethods)
                            AnalyzedMethods[hashcode] += ProcessMethodCallsInMethod(newMethod, n + 1);
                    }
                }
                catch (Exception ex)
                {

                    Logs.Console.Warn("Caught exception while processing method call node: {0} @ {1}", node, ex.Message);

                    if (!(ex is InvalidProjectFileException ||
                          ex is FormatException ||
                          ex is ArgumentException ||
                          ex is PathTooLongException))
                        throw;
                }
               
            }
            else
                return AnalyzedMethods[hashcode];

            return sum;
        }
        
    }
}
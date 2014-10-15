using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;

namespace ConsultingAnalysis
{
    class ComplexPatternDetectionWalker : CSharpSyntaxWalker
    {


        public ConsultingAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        public bool IsEventHandlerWalkerEnabled { get; set; }

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
                if (symbol.ToString().Contains("System.Threading.Tasks.Task.WaitAll"))
                {
                    var block = node.Ancestors().OfType<BlockSyntax>().First();

                    if (block.DescendantNodes().OfType<ForEachStatementSyntax>().Any() || block.DescendantNodes().OfType<ForStatementSyntax>().Any())
                        return;
                    foreach (var invocation in block.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        var symbol2 = (IMethodSymbol)SemanticModel.GetSymbolInfo(invocation).Symbol;
                        if (symbol2 != null && symbol2.IsTaskCreationMethod())
                        {
                            Logs.TempLog3.Info("{0}{1}--------------------------", Document.FilePath, block.ToLog());
                            break;
                        }
                    }

                }

            }


            base.VisitInvocationExpression(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(invocation).Symbol;
                if (symbol != null && symbol.IsTaskCreationMethod())
                {
                    Logs.TempLog.Info("{0}{1}--------------------------", Document.FilePath, node.Parent.ToLog());
                    break;
                }
                if (symbol != null && symbol.IsThreadStart())
                {
                    Logs.TempLog4.Info("{0}{1}--------------------------", Document.FilePath, node.Parent.ToLog());
                    break;
                }
            }

            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(invocation).Symbol;
                if (symbol != null && symbol.IsTaskCreationMethod())
                {
                    Logs.TempLog2.Info("{0}{1}--------------------------", Document.FilePath, node.Parent.ToLog());
                    break;
                }
                if (symbol != null && symbol.IsThreadStart())
                {
                    Logs.TempLog5.Info("{0}{1}--------------------------", Document.FilePath, node.Parent.ToLog());
                    break;
                }

            }
            base.VisitForEachStatement(node);
        }

    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;

namespace Analysis
{
    class AsyncLibraryDetectionWalker : CSharpSyntaxWalker
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
                Logs.ErrorLog.Info(@"{0} WCF is DETECTED", Result.SolutionPath);
            }
            else
                base.VisitClassDeclaration(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                IsAsyncLibraryConstruct(symbol.OriginalDefinition);
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var symbol = (IMethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            if (symbol != null)
            {
                IsAsyncLibraryConstruct(symbol.OriginalDefinition);
            }

            base.VisitObjectCreationExpression(node);
        }

        public void IsAsyncLibraryConstruct(IMethodSymbol symbol)
        {
            if (symbol.ContainingNamespace.ToString().Equals("System.Threading.Tasks") ||
                symbol.ContainingNamespace.ToString().Equals("System.Threading") ||
                (symbol.ContainingNamespace.ToString().Equals("System.Linq") && (symbol.ContainingType.ToString().Contains("ParallelQuery") || symbol.ContainingType.ToString().Contains("ParallelEnumerable"))) ||
                symbol.ContainingNamespace.ToString().Equals("System.Collections.Concurrent"))
            {

                if (ConsultingAnalysisResult.libraryUsage.ContainsKey(symbol.ToString()))
                    ConsultingAnalysisResult.libraryUsage[symbol.ToString()]++;
                else
                    ConsultingAnalysisResult.libraryUsage[symbol.ToString()] = 1;
            }
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            string name = "LOCK_Statement";

            if (ConsultingAnalysisResult.libraryUsage.ContainsKey(name))
                ConsultingAnalysisResult.libraryUsage[name]++;
            else
                ConsultingAnalysisResult.libraryUsage[name] = 1;
            base.VisitLockStatement(node);
        }
    }
}

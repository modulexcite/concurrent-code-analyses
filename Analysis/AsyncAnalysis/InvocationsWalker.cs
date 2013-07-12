using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;

namespace Analysis
{
    internal class InvocationsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis { get; set; }
        public AsyncAnalysisResult Result { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public IDocument Document { get; set; }

        private bool uiClass;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !uiClass)
            {
                uiClass = true;
                Result.NumUIClasses++;
            }
            base.VisitUsingDirective(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;

            var type = Analysis.DetectAsyncProgrammingUsages(node, symbol);

            Result.StoreDetectedAsyncUsage(type);

            Result.WriteDetectedAsync(type, Document.FilePath, symbol);

            base.VisitInvocationExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasAsyncModifier())
            {
                if (node.ReturnType.ToString().Equals("void"))
                {
                    if (node.HasEventArgsParameter())
                        Result.NumAsyncVoidEventHandlerMethods++;
                    else
                        Result.NumAsyncVoidNonEventHandlerMethods++;
                }
                else
                    Result.NumAsyncTaskMethods++;
            }

            base.VisitMethodDeclaration(node);
        }
    }
}
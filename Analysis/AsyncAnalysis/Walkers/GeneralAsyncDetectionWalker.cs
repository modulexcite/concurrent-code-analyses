using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utilities;

namespace Analysis
{
    internal class GeneralAsyncDetectionWalker : CSharpSyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public Document Document { get; set; }

        private bool uiClass;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !uiClass)
            {
                uiClass = true;
                Result.generalAsyncResults.NumUIClasses++;
            }
            base.VisitUsingDirective(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasEventArgsParameter())
                Result.generalAsyncResults.NumEventHandlerMethods++;

            base.VisitMethodDeclaration(node);
        }
    }
}
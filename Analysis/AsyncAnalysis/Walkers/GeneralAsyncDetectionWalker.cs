using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace Analysis
{
    internal class GeneralAsyncDetectionWalker : SyntaxWalker
    {
        public AsyncAnalysisResult Result { get; set; }

        public SemanticModel SemanticModel { get; set; }

        public IDocument Document { get; set; }

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
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace Analysis
{
    internal class AsyncAnalysisWalker : SyntaxWalker
    {
        private readonly AsyncProjectAnalysis _analysis;
        private readonly AsyncProjectAnalysisSummary _summary;

        private bool _ui;

        public AsyncAnalysisWalker(AsyncProjectAnalysis analysis, AsyncProjectAnalysisSummary summary)
        {
            _analysis = analysis;
            _summary = summary;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !_ui)
            {
                _ui = true;
                _summary.NumUIClasses++;
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasEventArgsParameter())
            {
                _summary.NumEventHandlerMethods++;
                _analysis.ProcessMethodCallsInMethod(node, 0);
            }

            if (node.HasAsyncModifier())
            {
                _summary.NumAsyncMethods++;
            }
        }
    }
}
using System.Linq;
using Roslyn.Compilers.CSharp;

namespace Analysis
{
    internal class AsyncAnalysisWalker : SyntaxWalker
    {
        private readonly AsyncProjectAnalysis _outer;
        private readonly AsyncProjectAnalysisSummary _summary;
        private bool _ui;

        public AsyncAnalysisWalker(AsyncProjectAnalysis outer, AsyncProjectAnalysisSummary summary)
        {
            _outer = outer;
            _summary = summary;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !_ui)
            {
                _ui = true;
                _summary.NumUIClasses++;
            }

            base.VisitUsingDirective(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.ParameterList.Parameters.Any(param => param.Type.ToString().EndsWith("EventArgs")))
            {
                _summary.NumEventHandlerMethods++;
                _outer.ProcessMethodCallsInMethod(node, 0);
            }
            // detect async methods
            if (node.Modifiers.ToString().Contains("async"))
                _summary.NumAsyncMethods++;

            base.VisitMethodDeclaration(node);
        }

    }
}
using Roslyn.Compilers.CSharp;

namespace Analysis
{
    internal class AsyncAnalysisWalker : SyntaxWalker
    {
        private readonly AsyncAnalysis _analysis;
        private readonly AsyncAnalysisResult _result;

        private bool _ui;

        public AsyncAnalysisWalker(AsyncAnalysis analysis, AsyncAnalysisResult result)
        {
            _analysis = analysis;
            _result = result;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.IsInSystemWindows() && !_ui)
            {
                _ui = true;
                _result.NumUIClasses++;
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasEventArgsParameter())
            {
                _result.NumEventHandlerMethods++;
                _analysis.ProcessMethodCallsInMethod(node, 0);
            }

            if (node.HasAsyncModifier())
            {
                _result.NumAsyncMethods++;
            }
        }
    }
}
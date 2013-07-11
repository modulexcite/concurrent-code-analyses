using Roslyn.Compilers.CSharp;

namespace Analysis
{
    internal class InvocationsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis { get; set; }
        public AsyncAnalysisResult Result { get; set; }
        public SemanticModel SemanticModel { get; set; }


        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            //Log.Trace("Visiting invocation expression: {0} @ {1}:{2}",node, _currentDocument.FilePath, node.GetLocation().GetLineSpan(false).StartLinePosition);

            var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;



            //if (pattern != null)
            //{
            //    Results.Info(@"{0},{1},{2},{3},{4},{5}",
            //                         _currentDocument.Project.Solution.FilePath,
            //                         _currentDocument.Project.FilePath,
            //                         _currentDocument.FilePath,
            //                         node.GetLocation()
            //                             .GetLineSpan(false)
            //                             .StartLinePosition.ToString()
            //                             .Replace(',', ':'),
            //                         symbol.ToString().Replace(',', ';'),
            //                         pattern);
            //}
        }
    }
}
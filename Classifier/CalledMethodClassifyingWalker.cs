using Analysis;
using NLog;
using Roslyn.Compilers.CSharp;
using Roslyn.Compilers.Common;
using Roslyn.Services;

namespace Classifier
{
    public class CalledMethodClassifyingWalker : SyntaxWalker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private IDocument _currentDocument;
        private ISemanticModel _semanticModel;

        public void Visit(IDocument document)
        {
            Log.Trace("Visiting document: {0}", document.FilePath);

            _currentDocument = document;
            _semanticModel = _currentDocument.GetSemanticModel();

            var node = document.GetSyntaxTree().GetRoot() as SyntaxNode;

            if (node != null)
            {
                Visit(node);
            }
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            Log.Trace("Visiting invocation expression: {0} @ {1}:{2}",
                      node,
                      _currentDocument.FilePath,
                      node.GetLocation().GetLineSpan(false).StartLinePosition);

            var symbol = (MethodSymbol)_semanticModel.GetSymbolInfo(node).Symbol;

            string pattern = null;
            if (node.IsEAPMethod())
            {
                pattern = "EAP";
            }
            else if (symbol.IsAPMBeginMethod())
            {
                pattern = "APM";
            }

            if (pattern != null)
            {
                Program.Results.Info(@"{0},{1},{2},{3},{4},{5}",
                                     _currentDocument.Project.Solution.FilePath,
                                     _currentDocument.Project.FilePath,
                                     _currentDocument.FilePath,
                                     node.GetLocation()
                                         .GetLineSpan(false)
                                         .StartLinePosition.ToString()
                                         .Replace(',', ':'),
                                     symbol.ToString().Replace(',', ';'),
                                     pattern);
            }
        }
    }
}
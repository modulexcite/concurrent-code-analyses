using Roslyn.Compilers.CSharp;
using Roslyn.Services;

namespace Analysis
{
    internal class InvocationsWalker : SyntaxWalker
    {
        public AsyncAnalysis Analysis { get; set; }
        public AsyncAnalysisResult Result { get; set; }
        public SemanticModel SemanticModel { get; set; }
        public IDocument Document { get; set; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            //Log.Trace("Visiting invocation expression: {0} @ {1}:{2}",node, _currentDocument.FilePath, node.GetLocation().GetLineSpan(false).StartLinePosition);

            var symbol = (MethodSymbol)SemanticModel.GetSymbolInfo(node).Symbol;


            var type = Analysis.DetectAsyncProgrammingUsages(node, symbol);

            Result.StoreDetectedAsyncUsage(type);

            var methodCallString = symbol.ToString(); ;

            if (symbol.ReturnsVoid)
                methodCallString = "void " + methodCallString;
            else
                methodCallString = symbol.ReturnType.ToString() + " " + methodCallString;
            
            Result.WriteDetectedAsync(type, Document.FilePath, methodCallString);

        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.HasAsyncModifier())
            {
                if (node.ReturnType.ToString().Equals("void"))
                    Result.NumAsyncVoidMethods++;
                else
                    Result.NumAsyncTaskMethods++;
            }
            

        }
    }
}
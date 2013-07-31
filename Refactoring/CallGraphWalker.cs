using System;
using Roslyn.Compilers.CSharp;
using Utilities;

namespace Refactoring
{
    public class CallGraphWalker
    {
        private readonly SemanticModel _model;

        public CallGraphWalker(SemanticModel model)
        {
            if (model == null) throw new ArgumentNullException("model");

            _model = model;
        }

        public void Walk(MethodDeclarationSyntax method)
        {
            if (method == null) throw new ArgumentNullException("method");

            var walker = new MethodBodyWalker(_model);
            walker.Visit(method.Body);
        }

        class MethodBodyWalker : SyntaxWalker
        {
            private readonly SemanticModel _model;

            public MethodBodyWalker(SemanticModel model)
            {
                if (model == null) throw new ArgumentNullException("model");

                _model = model;
            }

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                // Ignore lambdas
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                // Ignore lambdas
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                Console.WriteLine(@"Visiting node: {0}", node);

                var methodSymbol = _model.LookupMethodSymbol(node);
                var methodSyntax = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxNodes.First();

                Visit(methodSyntax.Body);
            }
        }
    }
}

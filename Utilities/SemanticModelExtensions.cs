using System;
using NLog;
using Roslyn.Compilers.CSharp;

namespace Utilities
{
    public static class SemanticModelExtensions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static MethodSymbol LookupMethodSymbol(this SemanticModel model, InvocationExpressionSyntax invocation)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (invocation == null) throw new ArgumentNullException("invocation");

            var expression = invocation.Expression;

            Logger.Trace("Looking up symbol for: {0}", expression);

            switch (expression.Kind)
            {
                case SyntaxKind.MemberAccessExpression:
                case SyntaxKind.IdentifierName:

                    var symbol = model.GetSymbolInfo(expression).Symbol;

                    if (symbol != null)
                    {
                        return (MethodSymbol)symbol;
                    }

                    throw new Exception("No symbol for invocation expression: " + invocation);

                default:
                    throw new NotImplementedException("Unsupported expression kind: " + expression.Kind + ": " + invocation);
            }
        }
    }
}

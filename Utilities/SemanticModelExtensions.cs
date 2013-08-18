using System;
using Roslyn.Compilers.CSharp;

namespace Utilities
{
    public static class SemanticModelExtensions
    {
        public static MethodSymbol LookupMethodSymbol(this SemanticModel model, InvocationExpressionSyntax invocation)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (invocation == null) throw new ArgumentNullException("invocation");

            var expression = invocation.Expression;
            Console.WriteLine(@"DEBUG: Looking up symbol for: {0}", expression);

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

        //public static TypeSymbol LookupTypeSymbol(this SemanticModel model, ExpressionSyntax expression)
        //{
        //    if (model == null) throw new ArgumentNullException("model");
        //    if (expression == null) throw new ArgumentNullException("expression");

        //    return model.GetTypeInfo(expression).Type;
        //}

        //public static Symbol LookupSymbol(this SemanticModel model, ExpressionSyntax expression)
        //{
        //    if (model == null) throw new ArgumentNullException("model");
        //    if (expression == null) throw new ArgumentNullException("expression");

        //    var symbolInfo = model.GetSymbolInfo(expression);

        //    if (symbolInfo.Symbol != null)
        //    {
        //        return symbolInfo.Symbol;
        //    }

        //    Console.WriteLine(@"No symbol found for expression: {0}", expression);
        //    return null;
        //}
    }
}

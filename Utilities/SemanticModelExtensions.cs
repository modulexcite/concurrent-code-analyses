using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Utilities
{
    public static class SemanticModelExtensions
    {
        public static MethodSymbol LookupMethodSymbol(this SemanticModel model, InvocationExpressionSyntax invocation)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (invocation == null) throw new ArgumentNullException("invocation");

            switch (invocation.Expression.Kind)
            {
                case SyntaxKind.PointerMemberAccessExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.IdentifierName:
                    var symbol = model.GetSymbolInfo(invocation.Expression).Symbol;
                    return (MethodSymbol)symbol;

                default:
                    throw new NotImplementedException(
                        String.Format(@"Unsupported expression kind: {0}: {1}", invocation.Expression.Kind, invocation)
                    );
            }
        }

        public static TypeSymbol LookupTypeSymbol(this SemanticModel model, ExpressionSyntax expression)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (expression == null) throw new ArgumentNullException("expression");

            return model.GetTypeInfo(expression).Type;
        }

        public static Symbol LookupSymbol(this SemanticModel model, ExpressionSyntax expression)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (expression == null) throw new ArgumentNullException("expression");

            var symbolInfo = model.GetSymbolInfo(expression);

            if (symbolInfo.Symbol != null)
            {
                return symbolInfo.Symbol;
            }

            Console.WriteLine(@"No symbol found for expression: {0}", expression);
            return null;
        }
    }
}
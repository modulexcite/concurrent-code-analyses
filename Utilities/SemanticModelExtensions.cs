﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System;

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
                case SyntaxKind.PointerMemberAccessExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.IdentifierName:

                    var symbol = model.GetSymbolInfo(expression).Symbol;

                    var methodSymbol = symbol as MethodSymbol;
                    if (methodSymbol != null)
                    {
                        return methodSymbol;
                    }

                    throw new MethodSymbolMissingException(invocation);

                default:
                    throw new NotImplementedException("Unsupported expression kind: " + expression.Kind + ": " + invocation);
            }
        }
    }

    public class SymbolMissingException : Exception
    {
        public SymbolMissingException(String message)
            : base(message)
        {
        }
    }

    public class MethodSymbolMissingException : SymbolMissingException
    {
        public MethodSymbolMissingException(InvocationExpressionSyntax invocation)
            : base("No method symbol found for invocation: " + invocation)
        {
        }
    }
}

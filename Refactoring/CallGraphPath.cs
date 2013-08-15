using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refactoring
{
    internal class CallGraphPath
    {
        private readonly InvocationExpressionSyntax _syntaxNode;

        public CallGraphPath(InvocationExpressionSyntax syntaxNode)
        {
            if (syntaxNode == null) throw new ArgumentNullException("syntaxNode");

            _syntaxNode = syntaxNode;
        }
    }
}
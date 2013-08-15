using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

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
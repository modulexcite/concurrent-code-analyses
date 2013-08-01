using System;
using Roslyn.Compilers.CSharp;

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
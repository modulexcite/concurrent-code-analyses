using System;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Refactoring
{
    public static class RefactoringExtensions
    {
        /// <summary>
        /// Execute the APM-to-async/await refactoring for a given APM method invocation.
        /// </summary>
        /// <param name="syntax">The CompilationUnitSyntax node on which to operate/in which the Begin and End method calls are represented.</param>
        /// <param name="invocation">The actual invocation of a BeginXXX APM method that marks which APM Begin/End pair must be refactored.</param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorAPMToAsyncAwait(this CompilationUnitSyntax syntax, InvocationExpressionSyntax invocation)
        {
            var method = invocation.ContainingMethod();

            if (invocation.Expression.Kind != SyntaxKind.MemberAccessExpression)
            {
                throw new NotImplementedException("Only member access expressions are supported");
            }

            var expression = (MemberAccessExpressionSyntax)invocation.Expression;

            if (!expression.Name.ToString().StartsWith("Begin"))
            {
                throw new NotImplementedException("Only invocations of methods starting with Begin are supported");
            }

            Console.WriteLine("Method: {0}", method);
            Console.WriteLine("Expression: {0}: {1}", expression, expression.Kind);
            Console.WriteLine("Name: {0}", expression.Name);

            var name = Syntax.IdentifierName("OtherName");
            var newExpression = expression.WithName(name);

            return syntax.ReplaceNode(expression, newExpression);
        }

        /// <summary>
        /// Returns the method containing this invocation statement.
        /// </summary>
        /// This invocation statement is contained in the scope of a certain method.
        /// The MethodDeclarationSyntax node of this method will be returned.
        /// <param name="invocation">The invocation statement</param>
        /// <returns>The MethodDeclarationSyntax node of the method that contains the given invocation statement.</returns>
        public static MethodDeclarationSyntax ContainingMethod(this InvocationExpressionSyntax invocation)
        {
            var node = invocation.Parent;

            while (!(node is MethodDeclarationSyntax))
            {
                node = node.Parent;
            }

            return (MethodDeclarationSyntax)node;
        }
    }
}

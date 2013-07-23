using System;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Formatting;

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
            var expression = (MemberAccessExpressionSyntax)invocation.Expression;

            // Check whether there is a callback parameter 
            if (HasCallbackParameter(invocation))
            {
                // annotate invocation expression
                var newMethod = CreateNewCallbackMethod(invocation);

                syntax = UpdateClassWithNewMethod(syntax, newMethod);

                
                //reassign invocation 
                TransformCallerMethod(invocation);
            }
            else
            { 
                // find the blocking call in the project where the endxxx is called.
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

        /// <summary>
        /// Checks whether the invocation satisfies the preconditions needed for async transformation
        /// </summary>
        /// <param name="invocation">The invocation statement</param>
        /// <returns>Returns true if it satisfies the preconditions, false if not</returns>
        public static bool IsAPMCandidateForAsync(this InvocationExpressionSyntax invocation)
        {
            var expression = (MemberAccessExpressionSyntax)invocation.Expression;
            if (invocation.Expression.Kind != SyntaxKind.MemberAccessExpression)
            {
                throw new NotImplementedException("Only member access expressions are supported");
            }

            if (!expression.Name.ToString().StartsWith("Begin"))
            {
                throw new NotImplementedException("Only invocations of methods starting with Begin are supported");
            }
            //throw new NotImplementedException(); 
            return true;
        }

        /// <summary>
        /// Checks whether the APM invocation has a callback function as a parameter to be called after the completion
        /// </summary>
        /// <param name="invocation">The APM invocation statement</param>
        /// <returns>Returns true if it has a callback function as a param, false if not</returns>
        public static bool HasCallbackParameter(this InvocationExpressionSyntax apmInvocation)
        {
            //throw new NotImplementedException(); //commented out just because it should not cause exceptions in the toy transformation trials. 
            return true;
        }

        private static void TransformCallerMethod(InvocationExpressionSyntax invocation)
        {
            throw new NotImplementedException();
        }

        private static MethodDeclarationSyntax CreateNewCallbackMethod(InvocationExpressionSyntax invocation)
        {

            MethodDeclarationSyntax newMethodDeclaration =
                Syntax.MethodDeclaration(Syntax.ParseTypeName("void"), "M")
                    .WithBody(Syntax.Block());

            return newMethodDeclaration; 
            
        }

        private static CompilationUnitSyntax UpdateClassWithNewMethod(CompilationUnitSyntax syntax, MethodDeclarationSyntax newMethod)
        {
            ClassDeclarationSyntax classDeclaration = syntax.ChildNodes()
    .OfType<ClassDeclarationSyntax>().Single();

            // Add this new MethodDeclarationSyntax to the above ClassDeclarationSyntax.
            ClassDeclarationSyntax newClassDeclaration =
                classDeclaration.AddMembers(newMethod);

            // Update the CompilationUnitSyntax with the new ClassDeclarationSyntax.
            CompilationUnitSyntax newSyntax =
                syntax.ReplaceNode(classDeclaration, newClassDeclaration);

            // Format the new CompilationUnitSyntax.
            //return (CompilationUnitSyntax)newSyntax.Format(FormattingOptions.GetDefaultOptions()).GetFormattedRoot();

            return newSyntax;
        }


    }
}


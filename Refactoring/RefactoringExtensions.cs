using System;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Formatting;
using Roslyn.Services;

namespace Refactoring
{
    public static class RefactoringExtensions
    {
        /// <summary>
        /// Execute the APM-to-async/await refactoring for a given APM method invocation.
        /// </summary>
        /// <param name="syntax">The CompilationUnitSyntax node on which to operate/in which the Begin and End method calls are represented.</param>
        /// <param name="apmInvocation">The actual invocation of a BeginXXX APM method that marks which APM Begin/End pair must be refactored.</param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorAPMToAsyncAwait(this CompilationUnitSyntax syntax, 
            InvocationExpressionSyntax apmInvocation,
            SemanticModel model)
        {
            var oldAPMContainingMethodDeclaration = apmInvocation.ContainingMethod();
            

            CompilationUnitSyntax newRoot=null; 


            // Check whether there is a callback parameter 
            if (apmInvocation.HasCallbackParameter())
            {
                var oldCallbackMethodDeclaration = FindCallbackMethod(apmInvocation, model);
                var newCallbackMethodDeclaration = CreateNewCallbackMethod(oldCallbackMethodDeclaration, model);



                var paramIdentifier = apmInvocation.ArgumentList.Arguments.Last().ToString();
                SyntaxList<StatementSyntax> list = oldAPMContainingMethodDeclaration.Body.Statements;
                list = list.Add(Syntax.ParseStatement("var result= task.ConfigureAwait(false).GetAwaiter().GetResult();\r\n"));
                list = list.Add(Syntax.ParseStatement("Callback("+ paramIdentifier +",result);"));


                var newAsyncMethodDeclaration = oldAPMContainingMethodDeclaration.WithModifiers( Syntax.ParseToken(oldAPMContainingMethodDeclaration.Modifiers.ToString()+" async")).WithBody(Syntax.Block(list));


                newRoot= (CompilationUnitSyntax) syntax.ReplaceNodes(oldNodes: new[] { oldCallbackMethodDeclaration, oldAPMContainingMethodDeclaration },
                              computeReplacementNode: (oldNode, newNode) =>
                                {
                                    if (oldNode == oldCallbackMethodDeclaration)
                                        return newCallbackMethodDeclaration;
                                    else if (oldNode == oldAPMContainingMethodDeclaration)
                                        return newAsyncMethodDeclaration;
                                    return null;
                                }
                              ).Format(FormattingOptions.GetDefaultOptions()).GetFormattedRoot();

                Console.WriteLine(newRoot);
            }
            else
            {
                // find the blocking call in the project where the endxxx is called.
                throw new NotImplementedException();
            }


            return newRoot;
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
            return true;
        }


        private static MethodDeclarationSyntax CreateNewCallbackMethod(MethodDeclarationSyntax oldMethodDeclaration, SemanticModel model)
        {

            var oldMethodBody = oldMethodDeclaration.Body;

            //Console.WriteLine("parameter "+oldMethodDeclaration.ParameterList);

            var identifierIAsyncResult= oldMethodDeclaration.ParameterList.ChildNodes().OfType<ParameterSyntax>().Where(a => a.Type.ToString().Equals("IAsyncResult")).First().Identifier;

            //Console.WriteLine("id: "+identifierIAsyncResult);

            var localDeclarationList= oldMethodBody.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Where(a => a.ToString().Contains(identifierIAsyncResult.ToString()));


            string parameterListText = "(";
            int c = 0;
            foreach (var stmt in localDeclarationList)
            {
                var expression = stmt.Declaration.Variables.First().Initializer.Value;
                var id = stmt.Declaration.Variables.First().Identifier;
                var type = model.GetTypeInfo(expression).Type;
                
                if(c!=0)
                    parameterListText+=", ";

                parameterListText += type.ToString() + " " + id.ToString();
                //Console.WriteLine("* "+ type + " " + id);
                // * System.Net.WebRequest request,  System.Net.WebResponse response
                c++;
            }
            parameterListText += ")";

            var newMethodBody = oldMethodBody.RemoveNodes(localDeclarationList,SyntaxRemoveOptions.KeepNoTrivia);
                                                                                                
            MethodDeclarationSyntax newMethodDeclaration =
                Syntax.MethodDeclaration( oldMethodDeclaration.ReturnType, oldMethodDeclaration.Identifier.ToString())
                .WithModifiers( oldMethodDeclaration.Modifiers)
                .WithParameterList(Syntax.ParseParameterList(parameterListText))    
                .WithBody(newMethodBody);


            return newMethodDeclaration;
        }

        private static MethodDeclarationSyntax FindCallbackMethod(InvocationExpressionSyntax invocation, SemanticModel model)
        {
            MethodSymbol symbol =(MethodSymbol) model.GetSymbolInfo(invocation).Symbol;

            int c = 0;
            int numCallbackParam = 0;
            foreach (var arg in symbol.Parameters)
            {
                if (arg.ToString().Contains("AsyncCallback"))
                {
                    numCallbackParam = c;
                    break; ;
                }
                c++;
            }

            c = 0; 
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                if (c == numCallbackParam)
                {
                    if (arg.Expression.Kind.ToString().Contains("IdentifierName"))
                    {
                        var methodSymbol = model.GetSymbolInfo(arg.Expression).Symbol;

                        return (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxNodes.First();
                    }
                }
                c++;
            }
            return null; 
        }


      

    }
}


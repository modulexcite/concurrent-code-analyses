using System;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Formatting;
using Roslyn.Services;
using Utilities;

namespace Refactoring
{
    public static class RefactoringExtensions
    {
        /// <summary>
        /// Execute the APM-to-async/await refactoring for a given APM method invocation.
        /// </summary>
        /// <param name="syntax">The CompilationUnitSyntax node on which to operate/in which the Begin and End method calls are represented.</param>
        /// <param name="apmStatement">The actual invocation of a BeginXXX APM method that marks which APM Begin/End pair must be refactored.</param>
        /// <param name="model">The semantic model representation that corresponds to the compiled version of the compilation unit.</param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorAPMToAsyncAwait(this CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model)
        {
            if (syntax == null)
                throw new NullReferenceException("syntax");
            if (apmStatement == null)
                throw new NullReferenceException("apmStatement");
            if (model == null)
                throw new NullReferenceException("model");

            var oldAPMContainingMethodDeclaration = apmStatement.ContainingMethod();
            CompilationUnitSyntax newRoot = null;

            if (apmStatement.HasAsyncCallbackParameter(model))
            {
                var oldCallbackMethodDeclaration = apmStatement.FindCallbackMethod(model);
                var newCallbackMethodDeclaration = CreateNewCallbackMethod(oldCallbackMethodDeclaration, model);
                var newAsyncMethodDeclaration = NewAsyncMethodDeclaration(apmStatement, oldAPMContainingMethodDeclaration);

                newRoot = (CompilationUnitSyntax)syntax.ReplaceNodes(oldNodes: new[] { oldCallbackMethodDeclaration, oldAPMContainingMethodDeclaration },
                              computeReplacementNode: (oldNode, newNode) =>
                                {
                                    if (oldNode == oldCallbackMethodDeclaration)
                                        return newCallbackMethodDeclaration;
                                    else if (oldNode == oldAPMContainingMethodDeclaration)
                                        return newAsyncMethodDeclaration;
                                    return null;
                                }
                              ).Format(FormattingOptions.GetDefaultOptions()).GetFormattedRoot();

                //Console.WriteLine(newRoot);
            }
            else
            {
                // find the blocking call in the project where the endxxx is called.
                throw new NotImplementedException();
            }


            return newRoot;
        }

        private static MethodDeclarationSyntax NewAsyncMethodDeclaration(ExpressionStatementSyntax apmInvocation, MethodDeclarationSyntax apmMethod)
        {
            const string taskName = "task";

            var invocationExpression = ((InvocationExpressionSyntax)apmInvocation.Expression);
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            var objectName = memberAccessExpression.Expression.ToString();
            var methodName = AsyncMethodNameForAPMBeginInvocation(apmInvocation);

            var tapInvocation = ConfigureAwaitStatementSyntax(taskName, objectName, methodName);

            var asyncMethod = apmMethod.ReplaceNode(apmInvocation, tapInvocation);

            var paramIdentifier = invocationExpression.ArgumentList.Arguments.Last().ToString();
            var awaitCode = String.Format("var result = {0}.ConfigureAwait(false).GetAwaiter().GetResult();\n", taskName);
            asyncMethod = asyncMethod.AddBodyStatements(
                Syntax.ParseStatement(awaitCode),
                Syntax.ParseStatement("Callback(" + paramIdentifier + ", result);")
            );

            asyncMethod = asyncMethod.WithModifiers(
                asyncMethod.Modifiers.Add(
                    Syntax.Token(SyntaxKind.AsyncKeyword)
                )
            );

            return asyncMethod;
        }

        private static string AsyncMethodNameForAPMBeginInvocation(ExpressionStatementSyntax apmInvocation)
        {
            // TODO: Look up the actual symbols to make sure that they exist/the right (TAP) method is chosen.
            // An example of where things might go wrong is the fact that EAP
            // and TAP method names usually both end with Async, and if both
            // exist, the TAP version is named XxxTaskAsync.

            var expression = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)apmInvocation.Expression).Expression;

            var apmMethodName = expression.Name.ToString();
            var methodNameBase = apmMethodName.Substring(5);
            var tapMethodName = methodNameBase + "Async";

            return tapMethodName;
        }

        private static StatementSyntax ConfigureAwaitStatementSyntax(string taskName, string objectName, string methodName)
        {
            var code = String.Format("var {0} = {1}.{2}().ConfigureAwait(false);\n", taskName, objectName, methodName);

            return Syntax.ParseStatement(code);
        }

        /// <summary>
        /// Returns the method containing this invocation statement.
        /// </summary>
        /// This invocation statement is contained in the scope of a certain method.
        /// The MethodDeclarationSyntax node of this method will be returned.
        ///
        /// TODO: This method does not consider e.g. lambda expressions.
        ///
        /// <param name="statement">The expression statement</param>
        /// <returns>The MethodDeclarationSyntax node of the method that contains the given expression statement.</returns>
        public static MethodDeclarationSyntax ContainingMethod(this ExpressionStatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException("statement");

            var node = statement.Parent;

            while (!(node is MethodDeclarationSyntax))
            {
                node = node.Parent;
            }

            return (MethodDeclarationSyntax)node;
        }

        public static Enums.CallbackType DetectCallbackParameter(this ExpressionStatementSyntax statement, SemanticModel model)
        {
            var invocation = (InvocationExpressionSyntax)statement.Expression;
            var symbol = (MethodSymbol)model.GetSymbolInfo(invocation).Symbol;

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
                        return Enums.CallbackType.Identifier;
                }
                c++;
            }

            return Enums.CallbackType.None;
        }

        private static bool HasAsyncCallbackParameter(this ExpressionStatementSyntax statement, SemanticModel model)
        {
            var invocation = (InvocationExpressionSyntax)statement.Expression;
            var symbol = (MethodSymbol)model.GetSymbolInfo(invocation).Symbol;

            return symbol.Parameters
                .Any(arg => arg.Type.Name.Equals("AsyncCallback"));
        }

        private static MethodDeclarationSyntax CreateNewCallbackMethod(MethodDeclarationSyntax oldMethodDeclaration, SemanticModel model)
        {

            var oldMethodBody = oldMethodDeclaration.Body;

            //Console.WriteLine("parameter "+oldMethodDeclaration.ParameterList);

            var identifierIAsyncResult = oldMethodDeclaration.ParameterList.ChildNodes().OfType<ParameterSyntax>().Where(a => a.Type.ToString().Equals("IAsyncResult")).First().Identifier;

            //Console.WriteLine("id: "+identifierIAsyncResult);

            var localDeclarationList = oldMethodBody.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Where(a => a.ToString().Contains(identifierIAsyncResult.ToString()));


            string parameterListText = "(";
            int c = 0;
            foreach (var stmt in localDeclarationList)
            {
                var expression = stmt.Declaration.Variables.First().Initializer.Value;
                var id = stmt.Declaration.Variables.First().Identifier;
                var type = model.GetTypeInfo(expression).Type;

                if (c != 0)
                    parameterListText += ", ";

                parameterListText += type.ToString() + " " + id.ToString();
                //Console.WriteLine("* "+ type + " " + id);
                // * System.Net.WebRequest request,  System.Net.WebResponse response
                c++;
            }
            parameterListText += ")";

            var newMethodBody = oldMethodBody.RemoveNodes(localDeclarationList, SyntaxRemoveOptions.KeepNoTrivia);

            MethodDeclarationSyntax newMethodDeclaration =
                Syntax.MethodDeclaration(oldMethodDeclaration.ReturnType, oldMethodDeclaration.Identifier.ToString())
                .WithModifiers(oldMethodDeclaration.Modifiers)
                .WithParameterList(Syntax.ParseParameterList(parameterListText))
                .WithBody(newMethodBody);


            return newMethodDeclaration;
        }

        private static MethodDeclarationSyntax FindCallbackMethod(this ExpressionStatementSyntax statement, SemanticModel model)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)statement.Expression;
            MethodSymbol symbol = (MethodSymbol)model.GetSymbolInfo(invocation).Symbol;

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


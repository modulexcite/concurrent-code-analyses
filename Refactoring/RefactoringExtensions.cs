using System;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
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

            var actualArgumentKind = DetectActualCallbackArgumentKind(apmStatement, model);
            switch (actualArgumentKind)
            {
                case SyntaxKind.IdentifierName:
                    return RefactorInstanceWithMethodReferenceCallback(syntax, apmStatement, model);

                case SyntaxKind.SimpleLambdaExpression:
                    throw new NotImplementedException("Simple lambda is not yet supported");

                case SyntaxKind.ParenthesizedLambdaExpression:
                    return RefactorInstanceWithParenthesizedLambdaCallback(syntax, apmStatement, model);

                default:
                    throw new NotImplementedException("Unsupported actual argument syntax node kind: " + actualArgumentKind);
            }
        }

        private static CompilationUnitSyntax RefactorInstanceWithMethodReferenceCallback(CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model)
        {
            var apmMethod = apmStatement.ContainingMethod();

            var oldCallback = apmStatement.FindCallbackMethod(model);
            var newCallback = CreateNewCallbackMethod(oldCallback, model);
            var newCallingMethod = NewAsyncMethodDeclaration(apmStatement, apmMethod);

            return syntax.ReplaceAll(new[]
            {
                new SyntaxNodeReplacementPair(oldCallback, newCallback),
                new SyntaxNodeReplacementPair(apmMethod, newCallingMethod)
            }).Format();
        }

        private static CompilationUnitSyntax RefactorInstanceWithParenthesizedLambdaCallback(CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model)
        {
            var apmMethod = apmStatement.ContainingMethod();

            var invocation = (InvocationExpressionSyntax)apmStatement.Expression;
            var symbol = (MethodSymbol)model.GetSymbolInfo(invocation).Symbol;

            var callbackIndex = FindCallbackParamIndex(symbol);

            var memberAccessExpression = (MemberAccessExpressionSyntax)invocation.Expression;

            var objectName = memberAccessExpression.Expression.ToString();
            var methodNameBase = GetAsyncMethodNameBase(apmStatement);
            var methodName = methodNameBase + "Async";

            const string taskName = "task";
            var tapStatement = StatementSyntax(taskName, objectName, methodName);

            var lambda = (ParenthesizedLambdaExpressionSyntax)invocation.ArgumentList.Arguments.ElementAt(callbackIndex).Expression;
            switch (lambda.Body.Kind)
            {
                case SyntaxKind.Block:
                    var lambdaBlock = (BlockSyntax)lambda.Body;

                    var endStatement = FindEndXxxCallSyntaxNode(lambdaBlock, objectName, methodNameBase);
                    var awaitStatement = AwaitExpression(taskName);
                    lambdaBlock = lambdaBlock.ReplaceNode(endStatement, awaitStatement);

                    var newMethod = apmMethod.ReplaceNode(apmStatement, tapStatement)
                                             .AddBodyStatements(lambdaBlock.Statements.ToArray());

                    return syntax.ReplaceNode(apmMethod, newMethod)
                                 .Format();

                default:
                    // Might be any other SyntaxNode kind, such as InvocationExpression.
                    throw new NotImplementedException("Unsupported lambda body syntax node kind: " + lambda.Body.Kind + ": lambda: " + lambda);
            }
        }

        private static ExpressionSyntax AwaitExpression(string taskName)
        {
            // TODO: Use 'await' once available in the next CTP.
            var code = String.Format(@"{0}.GetAwaiter().GetResult()", taskName);

            return Syntax.ParseExpression(code);
        }

        private static SyntaxNode FindEndXxxCallSyntaxNode(BlockSyntax lambdaBlock, string objectName, string methodName)
        {
            // TODO: Check for correct signature, etc.
            // This can be done much smarter by e.g. using the BeginXxx method symbol, looking up the corresponding EndXxx symobl, and filtering on that.

            var endStatement = lambdaBlock.DescendantNodes()
                                          .OfType<MemberAccessExpressionSyntax>()
                                          .First(stmt => stmt.Name.ToString().Equals("End" + methodName));

            return endStatement.Parent;
        }

        private static MethodDeclarationSyntax NewAsyncMethodDeclaration(ExpressionStatementSyntax apmInvocation, MethodDeclarationSyntax apmMethod)
        {
            const string taskName = "task";

            var invocationExpression = ((InvocationExpressionSyntax)apmInvocation.Expression);
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            var objectName = memberAccessExpression.Expression.ToString();
            var methodName = AsyncMethodNameForAPMBeginInvocation(apmInvocation);

            var tapInvocation = StatementSyntax(taskName, objectName, methodName);

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

            var methodNameBase = GetAsyncMethodNameBase(apmInvocation);
            var tapMethodName = methodNameBase + "Async";

            return tapMethodName;
        }

        private static string GetAsyncMethodNameBase(ExpressionStatementSyntax apmInvocation)
        {
            var expression = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)apmInvocation.Expression).Expression;

            var apmMethodName = expression.Name.ToString();
            var methodNameBase = apmMethodName.Substring(5);
            return methodNameBase;
        }

        private static StatementSyntax StatementSyntax(string taskName, string objectName, string methodName)
        {
            var code = String.Format("var {0} = {1}.{2}();\n", taskName, objectName, methodName);

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

        private static SyntaxKind DetectActualCallbackArgumentKind(this ExpressionStatementSyntax statement, SemanticModel model)
        {
            var invocation = (InvocationExpressionSyntax)statement.Expression;
            var symbol = (MethodSymbol)model.GetSymbolInfo(invocation).Symbol;

            var callbackParamIndex = FindCallbackParamIndex(symbol);

            if (callbackParamIndex == -1)
                throw new Exception("Callback parameter number == -1");

            return invocation.ArgumentList.Arguments.ElementAt(callbackParamIndex).Expression.Kind;
        }

        private static int FindCallbackParamIndex(MethodSymbol symbol)
        {
            for (var i = 0; i < symbol.Parameters.Count; i++)
            {
                var parameter = symbol.Parameters.ElementAt(i);
                if (parameter.Type.ToDisplayString().Equals(@"System.AsyncCallback"))
                {
                    return i;
                }
            }

            return -1;
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


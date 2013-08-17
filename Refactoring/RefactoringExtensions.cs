using System;
using System.Collections.Generic;
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
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (apmStatement == null) throw new ArgumentNullException("apmStatement");
            if (model == null) throw new ArgumentNullException("model");

            var actualArgumentKind = DetectActualCallbackArgumentKind(apmStatement, model);
            switch (actualArgumentKind)
            {
                case SyntaxKind.IdentifierName:
                    return RefactorInstanceWithMethodReferenceCallback(syntax, apmStatement, model);

                case SyntaxKind.SimpleLambdaExpression:
                    return RefactorInstanceWithLambdaCallback(syntax, apmStatement, model, false);

                case SyntaxKind.ParenthesizedLambdaExpression:
                    return RefactorInstanceWithLambdaCallback(syntax, apmStatement, model, true);

                default:
                    throw new NotImplementedException("Unsupported actual argument syntax node kind: " + actualArgumentKind);
            }
        }

        private static CompilationUnitSyntax RefactorInstanceWithMethodReferenceCallback(CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (apmStatement == null) throw new ArgumentNullException("apmStatement");
            if (model == null) throw new ArgumentNullException("model");

            var apmMethod = apmStatement.ContainingMethod();

            var oldCallback = apmStatement.FindCallbackMethodDeclaration(model);
            var newCallback = CreateNewCallbackMethod(oldCallback, model);
            var newCallingMethod = NewAsyncMethodDeclaration(apmStatement, apmMethod);

            return syntax.ReplaceAll(new[]
            {
                new SyntaxNodeExtensions.ReplacementPair(oldCallback, newCallback),
                new SyntaxNodeExtensions.ReplacementPair(apmMethod, newCallingMethod)
            }).Format();
        }

        private static CompilationUnitSyntax RefactorInstanceWithLambdaCallback(CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model,
            bool isParenthesized)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (apmStatement == null) throw new ArgumentNullException("apmStatement");
            if (model == null) throw new ArgumentNullException("model");

            var apmMethod = apmStatement.ContainingMethod();

            var invocation = (InvocationExpressionSyntax)apmStatement.Expression;
            var symbol = model.LookupMethodSymbol(invocation);

            var callbackIndex = FindCallbackParamIndex(symbol);

            var memberAccessExpression = (MemberAccessExpressionSyntax)invocation.Expression;

            var objectName = memberAccessExpression.Expression.ToString();
            var methodNameBase = GetAsyncMethodNameBase(apmStatement);
            var methodName = methodNameBase + "Async";

            const string taskName = "task";
            var tapStatement = NewVariableDeclarationStatement(taskName, objectName, methodName);

            var lambda = invocation.ArgumentList.Arguments.ElementAt(callbackIndex).Expression;
            var lambdaBody = isParenthesized ? ((ParenthesizedLambdaExpressionSyntax)lambda).Body : ((SimpleLambdaExpressionSyntax)lambda).Body;

            switch (lambdaBody.Kind)
            {
                case SyntaxKind.Block:
                    var lambdaBlock = (BlockSyntax)lambdaBody;

                    var endStatement = TryFindEndXxxCallSyntaxNode(lambdaBlock, methodNameBase);

                    if (endStatement == null)
                    {
                        // Every method invocation might lead to the target EndXxx. Try to find it recursively.
                        // Once found, rewrite the methods in the invocation path, one by one.
                        // Finally, rewrite the originating method, and the method with the EndXxx statement.

                        var invocationPathToEndXxx = TryFindEndXxxCallGraphPath(lambdaBlock, methodNameBase, model);

                        // These two get special treatment.
                        var endXxxCall = invocationPathToEndXxx.First();
                        var initialCall = invocationPathToEndXxx.Last();

                        invocationPathToEndXxx.Remove(endXxxCall);
                        invocationPathToEndXxx.Remove(initialCall);

                        var taskType = model.LookupMethodSymbol(endXxxCall).ReturnType.Name;

                        var replacements = new List<SyntaxNodeExtensions.ReplacementPair>(invocationPathToEndXxx.Count);
                        foreach (var invocationOnPath in invocationPathToEndXxx)
                        {
                            var containingMethod = invocationOnPath.ContainingMethod();
                            var asyncMethod = MakeCallGraphPathComponentAsync(invocationOnPath, containingMethod, taskType);

                            Console.WriteLine("Rewritten as:\n{0}", asyncMethod.Format());

                            replacements.Add(new SyntaxNodeExtensions.ReplacementPair(containingMethod, asyncMethod));
                        }

                        Console.WriteLine("Rewriting originating method:\n{0}", apmMethod.Format());

                        var rewrittenLambdaBlock = RewriteOriginatingMethodLambdaBlock(isParenthesized, lambda, initialCall, taskName, lambdaBlock);

                        var newMethod = apmMethod.ReplaceNode(apmStatement, tapStatement)
                                                 .AddBodyStatements(rewrittenLambdaBlock.Statements.ToArray())
                                                 .AddModifiers(NewAsyncKeyword());

                        Console.WriteLine("Rewritten originating method:\n{0}", newMethod.Format());

                        replacements.Add(new SyntaxNodeExtensions.ReplacementPair(apmMethod, newMethod));

                        // TODO: Do something with method containing EndXxx call.

                        return syntax.ReplaceAll(replacements)
                                     .Format();
                    }
                    else
                    {
                        var awaitStatement = NewAwaitExpression(taskName);
                        lambdaBlock = lambdaBlock.ReplaceNode(endStatement, awaitStatement);

                        var newMethod = apmMethod.ReplaceNode(apmStatement, tapStatement)
                                                 .AddBodyStatements(lambdaBlock.Statements.ToArray())
                                                 .AddModifiers(NewAsyncKeyword());

                        return syntax.ReplaceNode(apmMethod, newMethod)
                                     .Format();
                    }

                default:
                    // Might be any other SyntaxNode kind, such as InvocationExpression.
                    throw new NotImplementedException("Unsupported lambda body syntax node kind: " + lambdaBody.Kind + ": lambda: " + lambda);
            }
        }

        private static BlockSyntax RewriteOriginatingMethodLambdaBlock(bool isParenthesized, ExpressionSyntax lambda, InvocationExpressionSyntax initialCall, string taskName, BlockSyntax lambdaBlock)
        {
            ParameterSyntax lambdaParam;
            if (isParenthesized)
            {
                var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)lambda;
                lambdaParam = parenthesizedLambda.ParameterList.Parameters.First();
            }
            else
            {
                var simpleLambda = (SimpleLambdaExpressionSyntax)lambda;
                lambdaParam = simpleLambda.Parameter;
            }

            var asyncResultRef = initialCall.ArgumentList.Arguments.First(arg => ((IdentifierNameSyntax)arg.Expression).Identifier.ValueText.Equals(lambdaParam.Identifier.ValueText));
            var taskRef = Syntax.IdentifierName(taskName);
            var awaitStatement = NewAwaitExpression(
                initialCall.ReplaceNode(
                    asyncResultRef,
                    Syntax.Argument(taskRef)
                )
            );

            lambdaBlock = lambdaBlock.ReplaceNode(initialCall, awaitStatement);
            return lambdaBlock;
        }

        private static MethodDeclarationSyntax MakeCallGraphPathComponentAsync(InvocationExpressionSyntax invocation, MethodDeclarationSyntax method, String taskType)
        {
            if (invocation == null) throw new ArgumentNullException("invocation");
            if (method == null) throw new ArgumentNullException("method");

            Console.WriteLine("Rewriting method:\n{0}", method.Format());

            var asyncResultParam = FindIAsyncResultParameter(method.ParameterList);
            const string taskName = "task";

            var returnType = NewGenericTaskWithArgumentType(method.ReturnType);

            var taskParam = NewTaskParameter(taskName, taskType);
            var parameterList = method.ParameterList.ReplaceNode(asyncResultParam, taskParam);

            var taskRef = Syntax.IdentifierName(taskName);

            var replacements = method.Body.DescendantNodes()
                                                        .OfType<IdentifierNameSyntax>()
                                                        .Where(id => id.Identifier.ValueText.Equals(asyncResultParam.Identifier.ValueText))
                                                        .Select(asyncResultRef => new SyntaxNodeExtensions.ReplacementPair(asyncResultRef, taskRef))
                                                        .ToList();
            replacements.Add(AwaitedReplacementForInvocation(invocation, asyncResultParam, taskRef));

            foreach (var replacement in replacements)
            {
                Console.WriteLine("Replacement: {0}: with: {1}", replacement.OldNode, replacement.NewNode);
            }

            var body = method.Body.ReplaceAll(replacements);

            return method.AddModifiers(NewAsyncKeyword())
                         .WithReturnType(returnType)
                         .WithParameterList(parameterList)
                         .WithBody(body);
        }

        private static SyntaxNodeExtensions.ReplacementPair AwaitedReplacementForInvocation(InvocationExpressionSyntax invocation, ParameterSyntax asyncResultParam, IdentifierNameSyntax taskRef)
        {
            var invocationAsyncResultRef = invocation.DescendantNodes()
                                                     .OfType<IdentifierNameSyntax>()
                                                     .First(id => id.Identifier.ValueText.Equals(asyncResultParam.Identifier.ValueText));

            var awaitReplacement = new SyntaxNodeExtensions.ReplacementPair(
                invocation,
                NewAwaitExpression(
                    invocation.ReplaceNode(invocationAsyncResultRef, taskRef)
                )
            );

            return awaitReplacement;
        }

        private static List<InvocationExpressionSyntax> TryFindEndXxxCallGraphPath(BlockSyntax block, String methodNameBase, SemanticModel model)
        {
            var endXxxNode = TryFindEndXxxCallSyntaxNode(block, methodNameBase);

            if (endXxxNode != null)
            {
                return new List<InvocationExpressionSyntax> { endXxxNode };
            }

            var candidates = block.DescendantNodes()
                                  .OfType<InvocationExpressionSyntax>()
                                  .Where(node => NodeIsNotContainedInLambdaExpression(node, block));

            foreach (var candidate in candidates)
            {
                var methodSymbol = model.LookupMethodSymbol(candidate);
                var methodSyntax = (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxNodes.First();
                var potentialPath = TryFindEndXxxCallGraphPath(methodSyntax.Body, methodNameBase, model);

                if (!potentialPath.Any()) continue;

                potentialPath.Add(candidate);
                return potentialPath;
            }

            return new List<InvocationExpressionSyntax>();
        }

        /// <summary>
        /// Check that the path from the given node to the top node does not contain a simple or parenthesized lambda expression.
        ///
        /// Note: when topNode is not an ancestor of node, behavior is undefined.
        /// </summary>
        /// <param name="node">Node to check</param>
        /// <param name="topNode">Top level node to check the path to</param>
        /// <returns>true if the node is not contained in a lambda expression</returns>
        private static bool NodeIsNotContainedInLambdaExpression(SyntaxNode node, SyntaxNode topNode)
        {
            while (node != null && node != topNode)
            {
                if (node.Kind == SyntaxKind.SimpleLambdaExpression ||
                    node.Kind == SyntaxKind.ParenthesizedLambdaExpression)
                {
                    return false;
                }

                node = node.Parent;
            }

            return true;
        }

        private static InvocationExpressionSyntax TryFindEndXxxCallSyntaxNode(BlockSyntax lambdaBlock, string methodNameBase)
        {
            if (lambdaBlock == null) throw new ArgumentNullException("lambdaBlock");
            if (methodNameBase == null) throw new ArgumentNullException("methodNameBase");

            // TODO: Check for correct signature, etc.
            // This can be done much smarter by e.g. using the BeginXxx method symbol, looking up the corresponding EndXxx symobl, and filtering on that.

            try
            {
                // TODO: Also considier IdentifierName EndXxx instances.
                var endStatement = lambdaBlock.DescendantNodes()
                                              .OfType<MemberAccessExpressionSyntax>()
                                              .First(stmt => stmt.Name.ToString().Equals("End" + methodNameBase));

                return (InvocationExpressionSyntax)endStatement.Parent;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static MethodDeclarationSyntax NewAsyncMethodDeclaration(ExpressionStatementSyntax apmInvocation, MethodDeclarationSyntax apmMethod)
        {
            if (apmInvocation == null) throw new ArgumentNullException("apmInvocation");
            if (apmMethod == null) throw new ArgumentNullException("apmMethod");

            const string taskName = "task";
            const string resultName = "result";

            var invocationExpression = ((InvocationExpressionSyntax)apmInvocation.Expression);
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            var objectName = memberAccessExpression.Expression.ToString();
            var methodName = AsyncMethodNameForAPMBeginInvocation(apmInvocation);

            var tapInvocation = NewVariableDeclarationStatement(taskName, objectName, methodName);

            var asyncMethod = apmMethod.ReplaceNode(apmInvocation, tapInvocation);

            var paramIdentifier = invocationExpression.ArgumentList.Arguments.Last().ToString();

            var responseDeclarationStatement = NewVariableDeclarationStatement(resultName, NewAwaitExpression(taskName));

            asyncMethod = asyncMethod.AddBodyStatements(
                responseDeclarationStatement,
                Syntax.ParseStatement("Callback(" + paramIdentifier + ", result);")
            );

            asyncMethod = asyncMethod.WithModifiers(
                asyncMethod.Modifiers.Add(NewAsyncKeyword())
            );

            return asyncMethod;
        }

        private static string AsyncMethodNameForAPMBeginInvocation(ExpressionStatementSyntax apmInvocation)
        {
            if (apmInvocation == null) throw new ArgumentNullException("apmInvocation");

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
            if (apmInvocation == null) throw new ArgumentNullException("apmInvocation");

            var expression = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)apmInvocation.Expression).Expression;

            var apmMethodName = expression.Name.ToString();
            var methodNameBase = apmMethodName.Substring(5);
            return methodNameBase;
        }

        /// <summary>
        /// Returns the method containing this node.
        /// </summary>
        /// This node is supposedly contained in the scope of a certain method.
        /// The MethodDeclarationSyntax node of that method will be returned.
        ///
        /// TODO: This method does not consider e.g. lambda expressions.
        ///
        /// <param name="node">The syntax node</param>
        /// <returns>The MethodDeclarationSyntax node of the method that contains the given syntax node.</returns>
        public static MethodDeclarationSyntax ContainingMethod(this SyntaxNode node)
        {
            if (node == null) throw new ArgumentNullException("node");

            var parent = node.Parent;

            while (parent != null)
            {
                if (parent.Kind == SyntaxKind.MethodDeclaration)
                {
                    return (MethodDeclarationSyntax)parent;
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static SyntaxKind DetectActualCallbackArgumentKind(this ExpressionStatementSyntax statement, SemanticModel model)
        {
            if (statement == null) throw new ArgumentNullException("statement");
            if (model == null) throw new ArgumentNullException("model");

            var invocation = (InvocationExpressionSyntax)statement.Expression;
            var symbol = model.LookupMethodSymbol(invocation);

            var callbackParamIndex = FindCallbackParamIndex(symbol);

            if (callbackParamIndex == -1)
                throw new Exception("Callback parameter number == -1");

            return invocation.ArgumentList.Arguments.ElementAt(callbackParamIndex).Expression.Kind;
        }

        private static int FindCallbackParamIndex(MethodSymbol symbol)
        {
            if (symbol == null) throw new ArgumentNullException("symbol");

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
            if (oldMethodDeclaration == null) throw new ArgumentNullException("oldMethodDeclaration");
            if (model == null) throw new ArgumentNullException("model");

            var oldMethodBody = oldMethodDeclaration.Body;

            var identifierIAsyncResult = oldMethodDeclaration.ParameterList.Parameters
                                                             .First(a => a.Type.ToString().Equals("IAsyncResult"))
                                                             .Identifier;

            var localDeclarationList = oldMethodBody.DescendantNodes()
                                                    .OfType<LocalDeclarationStatementSyntax>()
                                                    .Where(a => a.ToString().Contains(identifierIAsyncResult.ToString()));

            var parameters = new List<string>();
            foreach (var stmt in localDeclarationList)
            {
                var expression = stmt.Declaration.Variables.First().Initializer.Value;
                var id = stmt.Declaration.Variables.First().Identifier;
                var type = model.LookupTypeSymbol(expression);

                parameters.Add(type + " " + id);
            }
            var parameterListText = "(" + String.Join(", ", parameters) + ")";

            var newMethodBody = oldMethodBody.RemoveNodes(localDeclarationList, SyntaxRemoveOptions.KeepNoTrivia);

            return Syntax.MethodDeclaration(oldMethodDeclaration.ReturnType, oldMethodDeclaration.Identifier.ToString())
                         .WithModifiers(oldMethodDeclaration.Modifiers)
                         .WithParameterList(Syntax.ParseParameterList(parameterListText))
                         .WithBody(newMethodBody);
        }

        private static MethodDeclarationSyntax FindCallbackMethodDeclaration(this ExpressionStatementSyntax statement, SemanticModel model)
        {
            if (statement == null) throw new ArgumentNullException("statement");
            if (model == null) throw new ArgumentNullException("model");

            var invocation = (InvocationExpressionSyntax)statement.Expression;
            var symbol = model.LookupMethodSymbol(invocation);

            var callbackParamIndex = -1;
            for (var i = 0; i < symbol.Parameters.Count; i++)
            {
                if (symbol.Parameters.ElementAt(i).ToString().Contains("AsyncCallback"))
                {
                    callbackParamIndex = i;
                    break;
                }
            }

            var argumentExpression = invocation.ArgumentList.Arguments.ElementAt(callbackParamIndex).Expression;
            if (argumentExpression.Kind.ToString().Contains("IdentifierName"))
            {
                var methodSymbol = model.LookupSymbol(argumentExpression);

                return (MethodDeclarationSyntax)methodSymbol.DeclaringSyntaxNodes.First();
            }

            return null;
        }

        private static ParameterSyntax FindIAsyncResultParameter(ParameterListSyntax parameterList)
        {
            return parameterList.Parameters
                                .First(param => param.Type.ToString().Equals("IAsyncResult"));
        }

        private static StatementSyntax NewVariableDeclarationStatement(string variableName, string objectName, string methodName)
        {
            if (variableName == null) throw new ArgumentNullException("variableName");
            if (objectName == null) throw new ArgumentNullException("objectName");
            if (methodName == null) throw new ArgumentNullException("methodName");

            var code = String.Format("{0}.{1}()", objectName, methodName);

            return NewVariableDeclarationStatement(variableName, Syntax.ParseExpression(code));
        }

        private static StatementSyntax NewVariableDeclarationStatement(string resultName, ExpressionSyntax expression)
        {
            if (resultName == null) throw new ArgumentNullException("resultName");
            if (expression == null) throw new ArgumentNullException("expression");

            var awaitCode = String.Format("var {0} = {1};\n", resultName, expression);

            return Syntax.ParseStatement(awaitCode);
        }

        private static ExpressionSyntax NewAwaitExpression(ExpressionSyntax expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            // TODO: Use 'await' once available in the next CTP.
            var code = String.Format(@"{0}.GetAwaiter().GetResult()", expression);

            return Syntax.ParseExpression(code);
        }

        private static ExpressionSyntax NewAwaitExpression(string taskName)
        {
            if (taskName == null) throw new ArgumentNullException("taskName");

            // TODO: Use 'await' once available in the next CTP.
            var code = String.Format(@"{0}.GetAwaiter().GetResult()", taskName);

            return Syntax.ParseExpression(code);
        }

        private static SyntaxToken NewAsyncKeyword()
        {
            return Syntax.Token(SyntaxKind.AsyncKeyword);
        }

        private static ParameterSyntax NewTaskParameter(string taskName, string taskType)
        {
            if (taskName == null) throw new ArgumentNullException("taskName");

            return Syntax.Parameter(
                null,
                Syntax.TokenList(),
                NewGenericTaskWithArgumentType(
                    Syntax.ParseTypeName(taskType)
                ),
                Syntax.Identifier(taskName),
                null
            );
        }

        private static TypeSyntax NewGenericTaskWithArgumentType(TypeSyntax returnType)
        {
            if (returnType == null) throw new ArgumentNullException("returnType");

            return Syntax.GenericName(
                Syntax.Identifier("Task"),
                Syntax.TypeArgumentList(
                    Syntax.SeparatedList(
                        returnType
                    )
                )
            );
        }
    }
}

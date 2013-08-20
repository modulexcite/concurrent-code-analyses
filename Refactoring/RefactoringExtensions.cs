using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace Refactoring
{
    public static class RefactoringExtensions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Execute the APM-to-async/await refactoring for a given APM method invocation.
        /// </summary>
        /// <param name="syntaxTree">The SyntaxTree on which to operate/in which the Begin and End method calls are represented.</param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorAPMToAsyncAwait(this SyntaxTree syntaxTree)
        {
            if (syntaxTree == null) throw new ArgumentNullException("syntaxTree");

            Logger.Trace("### REFACTORING CODE:\n{0}\n### END OF CODE", syntaxTree.GetRoot().Format());

            var compilation = CompilationUtils.CreateCompilation(syntaxTree);
            var model = compilation.GetSemanticModel(syntaxTree);
            var syntax = syntaxTree.GetRoot();

            var apmStatement = syntax.DescendantNodes()
                                     .OfType<ExpressionStatementSyntax>()
                                     .First(node => node.HasAnnotations<RefactorableAPMInstance>());

            var invocation = (InvocationExpressionSyntax)apmStatement.Expression;

            var callbackArgument = FindAsyncCallbackInvocationArgument(model, invocation);
            var callbackExpression = callbackArgument.Expression;

            switch (callbackExpression.Kind)
            {
                case SyntaxKind.IdentifierName:
                    return RefactorInstanceWithMethodReferenceCallbackAfterRewritingToSimpleLambda(syntax, apmStatement, model);

                case SyntaxKind.ParenthesizedLambdaExpression:
                    return RefactorInstanceWithParameterizedLambdaCallbackAfterRewritingToSimpleLambda(syntax, apmStatement, model);

                case SyntaxKind.SimpleLambdaExpression:
                    return RefactorInstanceWithSimpleLambdaCallback(syntax, model, apmStatement, (SimpleLambdaExpressionSyntax)callbackExpression);

                default:
                    throw new NotImplementedException(
                        "Unsupported actual argument syntax node kind: " + callbackExpression.Kind
                        + ": callback argument: " + callbackArgument
                    );
            }
        }

        private static CompilationUnitSyntax RefactorInstanceWithSimpleLambdaCallback(CompilationUnitSyntax syntax, SemanticModel model, ExpressionStatementSyntax apmStatement, SimpleLambdaExpressionSyntax lambda)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (model == null) throw new ArgumentNullException("model");
            if (apmStatement == null) throw new ArgumentNullException("apmStatement");
            if (lambda == null) throw new ArgumentNullException("lambda");

            if (lambda.Body.Kind == SyntaxKind.Block)
            {
                return RefactorSimpleLambdaInstance(syntax, apmStatement, model);
            }

            switch (lambda.Body.Kind)
            {
                case SyntaxKind.InvocationExpression:
                    return RefactoringSimpleLambdaInstanceAfterRewritingInvocationExpressionToBlock(syntax, model, apmStatement, lambda);

                default:
                    throw new NotImplementedException("Unsupported lambda body kind: " + lambda.Body.Kind + ": lambda: " +
                                                      lambda);
            }
        }

        private static CompilationUnitSyntax RefactoringSimpleLambdaInstanceAfterRewritingInvocationExpressionToBlock(CompilationUnitSyntax syntax, SemanticModel model, ExpressionStatementSyntax apmStatement, SimpleLambdaExpressionSyntax lambda)
        {
            var invocation = (InvocationExpressionSyntax)lambda.Body;

            var rewrittenSyntax = syntax.ReplaceNode(
                lambda.Body,
                NewBlock(
                    Syntax.ExpressionStatement(invocation)
                )
            );

            return SyntaxTree.Create(rewrittenSyntax)
                             .RefactorAPMToAsyncAwait();
        }

        private static CompilationUnitSyntax RefactorInstanceWithMethodReferenceCallbackAfterRewritingToSimpleLambda(CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (apmStatement == null) throw new ArgumentNullException("apmStatement");
            if (model == null) throw new ArgumentNullException("model");

            const string lambdaParamName = "result";

            var invocationExpression = ((InvocationExpressionSyntax)apmStatement.Expression);

            var callbackArgument = FindAsyncCallbackInvocationArgument(model, invocationExpression);

            var lambda = Syntax.SimpleLambdaExpression(
                             NewUntypedParameter(lambdaParamName),
                             NewBlock(
                                NewInvocationStatement(
                                    callbackArgument.Expression,
                                    NewSingletonArgumentList(
                                        Syntax.IdentifierName(lambdaParamName)
                                    )
                                )
                            )
                        );

            var lambdafiedSyntax = syntax.ReplaceNode(callbackArgument.Expression, lambda);

            return SyntaxTree.Create(lambdafiedSyntax)
                             .RefactorAPMToAsyncAwait();
        }

        private static CompilationUnitSyntax RefactorInstanceWithParameterizedLambdaCallbackAfterRewritingToSimpleLambda(CompilationUnitSyntax syntax, ExpressionStatementSyntax statement, SemanticModel model)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (statement == null) throw new ArgumentNullException("statement");

            var invocationExpression = ((InvocationExpressionSyntax)statement.Expression);

            var callbackArgument = FindAsyncCallbackInvocationArgument(model, invocationExpression);
            var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)callbackArgument.Expression;

            var simpleLambda = Syntax.SimpleLambdaExpression(
                parenthesizedLambda.ParameterList.Parameters.First(),
                parenthesizedLambda.Body
            );

            return SyntaxTree
                .Create(
                    syntax.ReplaceNode((SyntaxNode)parenthesizedLambda, simpleLambda)
                )
                .RefactorAPMToAsyncAwait();
        }

        private static CompilationUnitSyntax RefactorSimpleLambdaInstance(CompilationUnitSyntax syntax, ExpressionStatementSyntax apmStatement, SemanticModel model)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (apmStatement == null) throw new ArgumentNullException("apmStatement");
            if (model == null) throw new ArgumentNullException("model");

            var apmMethod = apmStatement.ContainingMethod();

            var invocationExpression = (InvocationExpressionSyntax)apmStatement.Expression;
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

            var objectName = memberAccessExpression.Expression.ToString();
            var methodNameBase = GetAsyncMethodNameBase(apmStatement);
            var methodName = methodNameBase + "Async";

            const string taskName = "task";
            var tapStatement = NewVariableDeclarationStatement(taskName, objectName, methodName);

            var callbackArgument = FindAsyncCallbackInvocationArgument(model, invocationExpression);
            var lambda = (SimpleLambdaExpressionSyntax)callbackArgument.Expression;

            if (lambda.Body.Kind != SyntaxKind.Block)
                throw new NotImplementedException("Unsupported lambda body syntax node kind: " + lambda.Body.Kind + ": lambda: " + lambda);

            var lambdaBlock = (BlockSyntax)lambda.Body;

            var endStatement = TryFindEndXxxCallSyntaxNode(lambdaBlock, methodNameBase);

            if (endStatement == null)
            {
                // Every method invocation might lead to the target EndXxx. Try to find it recursively.
                // Once found, rewrite the methods in the invocation path, one by one.
                // Finally, rewrite the originating method, and the method with the EndXxx statement.

                var invocationPathToEndXxx = TryFindCallGraphPathToEndXxx(lambdaBlock, methodNameBase, model);

                // These two get special treatment.
                var endXxxCall = invocationPathToEndXxx.First();
                var initialCall = invocationPathToEndXxx.Last();

                invocationPathToEndXxx.Remove(endXxxCall);
                invocationPathToEndXxx.Remove(initialCall);

                var endXxxMethod = model.LookupMethodSymbol(endXxxCall);
                var endXxxMethodReturnType = endXxxMethod.ReturnType;
                var taskType = endXxxMethodReturnType.Name;

                var replacements = new List<SyntaxNodeExtensions.ReplacementPair>(invocationPathToEndXxx.Count);

                var replacementPairs = from invocationOnPath in invocationPathToEndXxx
                                       let containingMethod = invocationOnPath.ContainingMethod()
                                       let asyncMethod = RewriteCallGraphPathComponent(
                                                             invocationOnPath,
                                                             containingMethod,
                                                             taskType
                                                         )
                                       select new SyntaxNodeExtensions.ReplacementPair(
                                           containingMethod,
                                           asyncMethod
                                       );
                replacements.AddRange(replacementPairs);

                var rewrittenLambdaBlock = RewriteOriginatingMethodLambdaBlock(lambda, initialCall, taskName);

                var newMethod = apmMethod.ReplaceNode(apmStatement, tapStatement)
                                         .AddBodyStatements(rewrittenLambdaBlock.Statements.ToArray())
                                         .AddModifiers(NewAsyncKeyword());

                replacements.Add(new SyntaxNodeExtensions.ReplacementPair(apmMethod, newMethod));

                var oldEndXxxContainingMethod = endXxxCall.ContainingMethod();
                var newEndXxxContainingMethod = RewriteEndXxxContainingMethod(oldEndXxxContainingMethod, endXxxCall, taskType);

                replacements.Add(new SyntaxNodeExtensions.ReplacementPair(oldEndXxxContainingMethod, newEndXxxContainingMethod));

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
        }

        /// <summary>
        /// Rewrite the originating method's lambda expression block so that its statements can be 'concatenated' to the originating method.
        /// </summary>
        /// <param name="lambda">The SimpleLambdaExpressionSyntax which must be rewritten.</param>
        /// <param name="callbackInvocation">The InvocationExpressionSyntax that represents the invocation of the callback in the lambda expression.</param>
        /// <param name="taskName">The name of the Task object that must be provided to the callback.</param>
        /// <returns>A rewritten BlockSyntax whose statements can be added to the originating method.</returns>
        private static BlockSyntax RewriteOriginatingMethodLambdaBlock(SimpleLambdaExpressionSyntax lambda, InvocationExpressionSyntax callbackInvocation, string taskName)
        {
            if (lambda == null) throw new ArgumentNullException("lambda");
            if (callbackInvocation == null) throw new ArgumentNullException("callbackInvocation");
            if (taskName == null) throw new ArgumentNullException("taskName");

            var asyncResultRefArg = callbackInvocation.ArgumentList.Arguments
                .First(arg => ((IdentifierNameSyntax)arg.Expression).Identifier.ValueText.Equals(lambda.Parameter.Identifier.ValueText));

            var awaitStatement = NewAwaitExpression(
                callbackInvocation.ReplaceNode(
                    asyncResultRefArg,
                    Syntax.Argument(
                        Syntax.IdentifierName(
                            taskName
                        )
                    )
                )
            );

            return ((BlockSyntax)lambda.Body).ReplaceNode(
                callbackInvocation,
                awaitStatement
            );
        }

        private static MethodDeclarationSyntax RewriteEndXxxContainingMethod(MethodDeclarationSyntax originalMethod, InvocationExpressionSyntax endXxxCall, string taskType)
        {
            const string taskName = "task";

            var returnType = NewTaskifiedReturnType(originalMethod);

            var replacements = new List<SyntaxNodeExtensions.ReplacementPair>();

            var asyncResultParameter = FindIAsyncResultParameter(originalMethod.ParameterList);
            var taskParameter = NewGenericTaskParameter(taskName, taskType);
            replacements.Add(new SyntaxNodeExtensions.ReplacementPair(asyncResultParameter, taskParameter));

            replacements.Add(new SyntaxNodeExtensions.ReplacementPair(
                endXxxCall,
                NewAwaitExpression(taskName)
            ));

            var asyncResultParamName = asyncResultParameter.Identifier.ValueText;

            var newMethod = originalMethod.ReplaceAll(replacements);
            var newMethodBody = newMethod.Body;

            // TODO: Use find-all-references, or manual data flow analysis.
            var nodes = from node in newMethodBody.DescendantNodes()
                                                  .OfType<LocalDeclarationStatementSyntax>()
                        where NodeIsNotContainedInLambdaExpression(node, newMethodBody)
                        where node.DescendantNodes()
                                  .OfType<IdentifierNameSyntax>()
                                  .Any(id => id.Identifier.ValueText.Equals(asyncResultParamName))
                        select node;

            newMethod = newMethod.RemoveNodes(nodes, SyntaxRemoveOptions.KeepNoTrivia);

            return newMethod
                .AddModifiers(NewAsyncKeyword())
                .WithReturnType(returnType);
        }

        private static MethodDeclarationSyntax RewriteCallGraphPathComponent(InvocationExpressionSyntax invocation, MethodDeclarationSyntax method, String taskType)
        {
            if (invocation == null) throw new ArgumentNullException("invocation");
            if (method == null) throw new ArgumentNullException("method");

            var asyncResultParam = FindIAsyncResultParameter(method.ParameterList);
            const string taskName = "task";

            var returnType = NewGenericTask(method.ReturnType);

            var taskParam = NewGenericTaskParameter(taskName, taskType);
            var parameterList = method.ParameterList.ReplaceNode(asyncResultParam, taskParam);

            var taskRef = SyntaxFactory.IdentifierName(taskName);

            var replacements = method.Body.DescendantNodes()
                                                        .OfType<IdentifierNameSyntax>()
                                                        .Where(id => id.Identifier.ValueText.Equals(asyncResultParam.Identifier.ValueText))
                                                        .Select(asyncResultRef => new SyntaxNodeExtensions.ReplacementPair(asyncResultRef, taskRef))
                                                        .ToList();
            replacements.Add(AwaitedReplacementForCallGraphComponentInvocation(invocation, asyncResultParam, taskRef));

            var body = method.Body.ReplaceAll(replacements);

            return method.AddModifiers(NewAsyncKeyword())
                         .WithReturnType(returnType)
                         .WithParameterList(parameterList)
                         .WithBody(body);
        }

        private static SyntaxNodeExtensions.ReplacementPair AwaitedReplacementForCallGraphComponentInvocation(InvocationExpressionSyntax invocation, ParameterSyntax asyncResultParam, IdentifierNameSyntax taskRef)
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

        private static List<InvocationExpressionSyntax> TryFindCallGraphPathToEndXxx(BlockSyntax block, String methodNameBase, SemanticModel model)
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
                var potentialPath = TryFindCallGraphPathToEndXxx(methodSyntax.Body, methodNameBase, model);

                if (!potentialPath.Any()) continue;

                potentialPath.Add(candidate);
                return potentialPath;
            }

            return new List<InvocationExpressionSyntax>();
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
                var endXxxExpression = lambdaBlock.DescendantNodes()
                                                  .OfType<MemberAccessExpressionSyntax>()
                                                  .First(stmt => stmt.Name.ToString().Equals("End" + methodNameBase));

                return (InvocationExpressionSyntax)endXxxExpression.Parent;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
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

        private static ArgumentSyntax FindInvocationArgument(SemanticModel model, InvocationExpressionSyntax invocation, string parameterTypeName)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (invocation == null) throw new ArgumentNullException("invocation");
            if (parameterTypeName == null) throw new ArgumentNullException("parameterTypeName");

            var symbol = model.LookupMethodSymbol(invocation);
            var parameterIndex = FindMethodParameterIndex(symbol, parameterTypeName);
            var callbackArgument = invocation.ArgumentList.Arguments.ElementAt(parameterIndex);

            return callbackArgument;
        }

        private static ArgumentSyntax FindAsyncCallbackInvocationArgument(SemanticModel model, InvocationExpressionSyntax invocation)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (invocation == null) throw new ArgumentNullException("invocation");

            const string parameterTypeName = "System.AsyncCallback";

            return FindInvocationArgument(model, invocation, parameterTypeName);
        }

        private static int FindMethodParameterIndex(MethodSymbol symbol, string parameterTypeName)
        {
            if (symbol == null) throw new ArgumentNullException("symbol");
            if (parameterTypeName == null) throw new ArgumentNullException("parameterTypeName");

            for (var i = 0; i < symbol.Parameters.Count(); i++)
            {
                var parameter = symbol.Parameters.ElementAt(i);
                if (parameter.Type.ToDisplayString().Equals(parameterTypeName))
                {
                    return i;
                }
            }

            throw new Exception("No " + parameterTypeName + " parameter found for method symbol: " + symbol);
        }

        //private static MethodDeclarationSyntax CreateNewCallbackMethod(MethodDeclarationSyntax oldMethodDeclaration, SemanticModel model)
        //{
        //    if (oldMethodDeclaration == null) throw new ArgumentNullException("oldMethodDeclaration");
        //    if (model == null) throw new ArgumentNullException("model");

        //    var oldMethodBody = oldMethodDeclaration.Body;

        //    var identifierIAsyncResult = oldMethodDeclaration.ParameterList.Parameters
        //                                                     .First(a => a.Type.ToString().Equals("IAsyncResult"))
        //                                                     .Identifier;

        //    var localDeclarationList = oldMethodBody.DescendantNodes()
        //                                            .OfType<LocalDeclarationStatementSyntax>()
        //                                            .Where(a => a.ToString().Contains(identifierIAsyncResult.ToString()));

        //    var parameters = new List<string>();
        //    foreach (var stmt in localDeclarationList)
        //    {
        //        var expression = stmt.Declaration.Variables.First().Initializer.Value;
        //        var id = stmt.Declaration.Variables.First().Identifier;
        //        var type = model.LookupTypeSymbol(expression);

        //        parameters.Add(type + " " + id);
        //    }
        //    var parameterListText = "(" + String.Join(", ", parameters) + ")";

        //    var newMethodBody = oldMethodBody.RemoveNodes(localDeclarationList, SyntaxRemoveOptions.KeepNoTrivia);

        //    return Syntax.MethodDeclaration(oldMethodDeclaration.ReturnType, oldMethodDeclaration.Identifier.ToString())
        //                 .WithModifiers(oldMethodDeclaration.Modifiers)
        //                 .WithParameterList(Syntax.ParseParameterList(parameterListText))
        //                 .WithBody(newMethodBody);
        //}

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
            var expression = Syntax.ParseExpression(code);

            return NewVariableDeclarationStatement(variableName, expression);
        }

        private static StatementSyntax NewVariableDeclarationStatement(string resultName, ExpressionSyntax expression)
        {
            if (resultName == null) throw new ArgumentNullException("resultName");
            if (expression == null) throw new ArgumentNullException("expression");

            var awaitCode = String.Format("var {0} = {1};\n", resultName, expression);

            return SyntaxFactory.ParseStatement(awaitCode);
        }

        private static ExpressionSyntax NewAwaitExpression(ExpressionSyntax expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            // TODO: Use 'await' once available in the next CTP.
            var code = String.Format(@"{0}.GetAwaiter().GetResult()", expression);

            return SyntaxFactory.ParseExpression(code);
        }

        private static ExpressionSyntax NewAwaitExpression(string taskName)
        {
            if (taskName == null) throw new ArgumentNullException("taskName");

            // TODO: Use 'await' once available in the next CTP.
            var code = String.Format(@"{0}.GetAwaiter().GetResult()", taskName);

            return SyntaxFactory.ParseExpression(code);
        }

        private static SyntaxToken NewAsyncKeyword()
        {
            return SyntaxFactory.Token(
                SyntaxKind.AsyncKeyword
            );
        }

        private static ParameterSyntax NewUntypedParameter(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            return Syntax.Parameter(
                null,
                Syntax.TokenList(),
                null,
                Syntax.Identifier(name),
                null
            );
        }

        private static TypeSyntax NewTaskifiedReturnType(MethodDeclarationSyntax originalMethod)
        {
            if (originalMethod == null) throw new ArgumentNullException("originalMethod");

            return originalMethod.ReturnsVoid()
                ? Syntax.ParseTypeName("Task")
                : NewGenericTask(originalMethod.ReturnType);
        }

        private static bool ReturnsVoid(this MethodDeclarationSyntax method)
        {
            return method.ReturnType.ToString().Equals("void");
        }

        private static ParameterSyntax NewGenericTaskParameter(string taskName, string parameterType)
        {
            if (taskName == null) throw new ArgumentNullException("taskName");

            return NewParameter(
                NewGenericTask(parameterType),
                Syntax.Identifier(taskName)
            );
        }

        private static ParameterSyntax NewParameter(TypeSyntax type, SyntaxToken name)
        {
            return Syntax.Parameter(
                null,
                Syntax.TokenList(),
                type,
                name,
                null
            );
        }

        private static TypeSyntax NewGenericTask(string parameterType)
        {
            if (parameterType == null) throw new ArgumentNullException("parameterType");

            return NewGenericTask(
                Syntax.ParseTypeName(parameterType)
            );
        }

        private static TypeSyntax NewGenericTask(TypeSyntax parameter)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");

            var identifier = Syntax.Identifier("Task");

            return NewGenericName(identifier, parameter);
        }

        private static GenericNameSyntax NewGenericName(SyntaxToken identifier, TypeSyntax returnType)
        {
            if (identifier == null) throw new ArgumentNullException("identifier");
            if (returnType == null) throw new ArgumentNullException("returnType");

            return SyntaxFactory.GenericName(
                identifier,
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(
                        returnType
                    )
                )
            );
        }

        private static BlockSyntax NewBlock<T>(params T[] statements) where T : StatementSyntax
        {
            if (statements == null) throw new ArgumentNullException("statements");

            return Syntax.Block(
                Syntax.List(
                    statements
                )
            );
        }

        private static ArgumentListSyntax NewSingletonArgumentList(ExpressionSyntax expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            return Syntax.ArgumentList(
                Syntax.SeparatedList(
                    Syntax.Argument(
                        expression
                    )
                )
            );
        }

        private static ExpressionStatementSyntax NewInvocationStatement(ExpressionSyntax expression, ArgumentListSyntax argumentList)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (argumentList == null) throw new ArgumentNullException("argumentList");

            return Syntax.ExpressionStatement(
                Syntax.InvocationExpression(
                    expression,
                    argumentList
                )
            );
        }
    }
}

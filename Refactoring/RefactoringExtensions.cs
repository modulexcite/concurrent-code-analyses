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
        /// <param name="document">The C# Document on which to operate/in which the Begin and End method calls are represented.</param>
        /// <param name="solution">The solution that contains the C# document.</param>
        /// <param name="workspace">The workspace to which the code in the syntax tree currently belongs, for formatting purposes.</param>
        /// <param name="index">The index number </param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorAPMToAsyncAwait(Document document, Solution solution, Workspace workspace, int index)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (workspace == null) throw new ArgumentNullException("workspace");

            var numErrorsInSolutionBeforeRewriting = solution.CompilationErrorCount();

            var syntaxTree = (SyntaxTree)document.GetSyntaxTreeAsync().Result;
            var syntax = (CompilationUnitSyntax)syntaxTree.GetRoot();

            Logger.Trace("\n### REFACTORING CODE ###\n{0}\n### END OF CODE ###", syntax.Format(workspace));

            InvocationExpressionSyntax beginXxxCall;
            try
            {
                beginXxxCall = document.GetAnnotatedInvocation(index);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(
                    "Syntax tree has no InvocationExpressionSyntax node annotated with RefactorableAPMInstance");
            }

            var model = (SemanticModel)document.GetSemanticModelAsync().Result;

            var callbackArgument = FindAsyncCallbackInvocationArgument(model, beginXxxCall);
            var callbackExpression = callbackArgument.Expression;

            CompilationUnitSyntax rewrittenSyntax;
            switch (callbackExpression.Kind)
            {
                case SyntaxKind.SimpleLambdaExpression:
                    var lambda = (SimpleLambdaExpressionSyntax)callbackExpression;

                    switch (lambda.Body.Kind)
                    {
                        case SyntaxKind.Block:
                            Logger.Info("Refactoring ...");
                            return RefactorSimpleLambdaInstance(syntax, beginXxxCall, model, workspace, callbackArgument);

                        case SyntaxKind.InvocationExpression:
                            Logger.Info("Rewriting lambda to block form ...");
                            rewrittenSyntax = RewriteInvocationExpressionToBlock(syntax, lambda, model, beginXxxCall);
                            break;

                        default:
                            throw new NotImplementedException("Unsupported lambda body kind: " + lambda.Body.Kind + ": lambda: " + lambda);
                    }
                    break;

                case SyntaxKind.IdentifierName:
                    var identifierName = (IdentifierNameSyntax)callbackExpression;
                    Logger.Info("Rewriting method reference to lambda ...");
                    rewrittenSyntax = RewriteMethodReferenceToSimpleLambda(syntax, beginXxxCall, model, callbackArgument, identifierName);
                    break;

                case SyntaxKind.ParenthesizedLambdaExpression:
                    Logger.Info("Rewriting parenthesized lambda to simple lambda ...");
                    rewrittenSyntax = RewriteParenthesizedLambdaToSimpleLambda(syntax, beginXxxCall, model);
                    break;

                case SyntaxKind.ObjectCreationExpression:
                    Logger.Info("Rewriting object creation expression to simple lambda ...");
                    rewrittenSyntax = RewriteObjectCreationToSimpleLambda(syntax, (ObjectCreationExpressionSyntax)callbackExpression, workspace);
                    break;

                default:
                    throw new NotImplementedException(
                        "Unsupported actual argument syntax node kind: " + callbackExpression.Kind
                        + ": callback argument: " + callbackArgument
                    );
            }

            var rewrittenDocument = document.WithSyntaxRoot(rewrittenSyntax);
            var rewrittenSolution = solution.WithDocumentSyntaxRoot(document.Id, rewrittenSyntax);

            if (rewrittenSolution.CompilationErrorCount() > numErrorsInSolutionBeforeRewriting)
            {
                const string message = "Rewritten solution contains more compilation errors than the original solution - not continuing";

                Logger.Warn(message);
                Logger.Warn("  APM Begin method call located at: {0}:{1}",
                    beginXxxCall.SyntaxTree.FilePath,
                    beginXxxCall.GetStartLineNumber()
                );
                Logger.Warn("\n### ORIGINAL CODE ###\n{0}### END OF CODE ###", syntax.Format(workspace));
                Logger.Warn("\n### REWRITTEN CODE ###\n{0}### END OF CODE ###", rewrittenSyntax.Format(workspace));

                throw new RefactoringException(message, beginXxxCall);
            }

            return RefactorAPMToAsyncAwait(rewrittenDocument, rewrittenSolution, workspace, index);
        }

        private static CompilationUnitSyntax RewriteInvocationExpressionToBlock(CompilationUnitSyntax syntax, SimpleLambdaExpressionSyntax lambda, SemanticModel model, InvocationExpressionSyntax beginXxxCall)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (lambda == null) throw new ArgumentNullException("lambda");
            if (model == null) throw new ArgumentNullException("model");
            if (beginXxxCall == null) throw new ArgumentNullException("beginXxxCall");

            var callbackInvocation = (InvocationExpressionSyntax)lambda.Body;

            var stateArgument = FindInvocationArgument(model, beginXxxCall, "object");
            var stateExpression = stateArgument.Expression;

            var originalCallbackMethodSymbol = model.LookupMethodSymbol(callbackInvocation);
            var originalCallbackMethod = (MethodDeclarationSyntax)originalCallbackMethodSymbol.DeclaringSyntaxReferences.First().GetSyntax();

            ArgumentListSyntax argumentList;
            MethodDeclarationSyntax rewrittenCallbackMethod;
            if (stateExpression.Kind == SyntaxKind.NullLiteralExpression)
            {
                argumentList = callbackInvocation.ArgumentList;
                rewrittenCallbackMethod = originalCallbackMethod;
            }
            else
            {
                argumentList = callbackInvocation.ArgumentList.AddArguments(
                    SyntaxFactory.Argument(stateExpression)
                );

                rewrittenCallbackMethod = RewriteCallbackWithIntroducedAsyncStateParameter(model, originalCallbackMethod, stateExpression);
            }

            var newLambdaBody = NewBlock(
                SyntaxFactory.ExpressionStatement(callbackInvocation.WithArgumentList(argumentList))
            );

            return syntax.ReplaceAll(
                new SyntaxReplacementPair(
                    lambda.Body,
                    newLambdaBody
                ),
                new SyntaxReplacementPair(
                    stateExpression,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                ),
                new SyntaxReplacementPair(
                    originalCallbackMethod,
                    rewrittenCallbackMethod
                )
            );
        }

        private static CompilationUnitSyntax RewriteMethodReferenceToSimpleLambda(CompilationUnitSyntax syntax, InvocationExpressionSyntax beginXxxCall, SemanticModel model, ArgumentSyntax callbackArgument, IdentifierNameSyntax identifierName)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (beginXxxCall == null) throw new ArgumentNullException("beginXxxCall");
            if (model == null) throw new ArgumentNullException("model");

            const string lambdaParamName = "result";

            var stateArgument = FindInvocationArgument(model, beginXxxCall, "object");
            var stateExpression = stateArgument.Expression;

            var lambdaParamRef = SyntaxFactory.IdentifierName(lambdaParamName);

            var originalCallbackMethodSymbol = model.LookupMethodSymbol(identifierName);
            var originalCallbackMethod = (MethodDeclarationSyntax)originalCallbackMethodSymbol.DeclaringSyntaxReferences.First().GetSyntax();

            ArgumentListSyntax argumentList;
            MethodDeclarationSyntax rewrittenCallbackMethod;
            if (stateExpression.Kind == SyntaxKind.NullLiteralExpression)
            {
                // TODO: Replace with NewArgumentList (that is untested!!!)
                argumentList = NewSingletonArgumentList(
                    lambdaParamRef
                );
                rewrittenCallbackMethod = originalCallbackMethod;
            }
            else
            {
                argumentList = NewArgumentList(
                    lambdaParamRef,
                    stateExpression
                );

                rewrittenCallbackMethod = RewriteCallbackWithIntroducedAsyncStateParameter(model, originalCallbackMethod, stateExpression);
            }

            var lambda = SyntaxFactory.SimpleLambdaExpression(
                             NewUntypedParameter(lambdaParamName),
                             NewBlock(
                                NewInvocationStatement(
                                    callbackArgument.Expression,
                                    argumentList
                                )
                             )
                         );

            return syntax.ReplaceAll(
                new SyntaxReplacementPair(
                    callbackArgument.Expression,
                    lambda
                ),
                new SyntaxReplacementPair(
                    stateExpression,
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NullLiteralExpression
                    )
                ),
                new SyntaxReplacementPair(
                    originalCallbackMethod,
                    rewrittenCallbackMethod
                )
            );
        }

        private static MethodDeclarationSyntax RewriteCallbackWithIntroducedAsyncStateParameter(SemanticModel model, MethodDeclarationSyntax originalCallbackMethod, ExpressionSyntax stateExpression)
        {
            var stateExpressionTypeSymbol = model.GetTypeInfo(stateExpression).Type;
            var newParameterTypeName = stateExpressionTypeSymbol.Name;

            var statement = originalCallbackMethod.Body
                .Statements
                .First(stmt => stmt.ToString().Contains("AsyncState"));

            SyntaxToken identifier;
            switch (statement.Kind)
            {
                case SyntaxKind.LocalDeclarationStatement:
                    var declaration = ((LocalDeclarationStatementSyntax)statement).Declaration;

                    if (declaration.Variables.Count != 1)
                        throw new NotImplementedException(
                            "AsyncState referenced in LocalDeclarationStatement with multiple variables: " + statement);

                    identifier = declaration.Variables.First().Identifier;

                    break;

                default:
                    throw new NotImplementedException("First statement that uses AsyncState has unknown kind: " + statement.Kind +
                                                      ": statement: " + statement);
            }

            return originalCallbackMethod.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia)
                                         .AddParameterListParameters(
                                             NewParameter(
                                                 SyntaxFactory.IdentifierName(newParameterTypeName),
                                                 identifier
                                             )
                                         );
        }

        private static CompilationUnitSyntax RewriteParenthesizedLambdaToSimpleLambda(CompilationUnitSyntax syntax, InvocationExpressionSyntax invocation, SemanticModel model)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (invocation == null) throw new ArgumentNullException("invocation");

            var callbackArgument = FindAsyncCallbackInvocationArgument(model, invocation);
            var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)callbackArgument.Expression;

            var simpleLambda = SyntaxFactory.SimpleLambdaExpression(
                parenthesizedLambda.ParameterList.Parameters.First(),
                parenthesizedLambda.Body
            );

            return syntax.ReplaceNode(
                (SyntaxNode)parenthesizedLambda,
                simpleLambda
            );
        }

        private static CompilationUnitSyntax RewriteObjectCreationToSimpleLambda(CompilationUnitSyntax syntax, ObjectCreationExpressionSyntax objectCreation, Workspace workspace)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (objectCreation == null) throw new ArgumentNullException("objectCreation");
            if (workspace == null) throw new ArgumentNullException("workspace");

            if (!objectCreation.Type.ToString().Equals("AsyncCallback"))
            {
                Logger.Error("Unknown ObjectCreation type in callback: {0}", objectCreation);

                throw new NotImplementedException("Unknown ObjectCreation type in callback: " + objectCreation);
            }

            var expression = objectCreation.ArgumentList.Arguments.First().Expression;

            switch (expression.Kind)
            {
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.IdentifierName:
                    return syntax.ReplaceNode(
                        (SyntaxNode)objectCreation,
                        expression
                    );

                default:
                    Logger.Error("Unsupported expression type as argument of AsyncCallback constructor: {0}: {1}", expression.Kind, objectCreation);

                    throw new NotImplementedException("Unsupported expression type as argument of AsyncCallback constructor: " + expression.Kind + ": " + objectCreation);
            }
        }

        private static CompilationUnitSyntax RefactorSimpleLambdaInstance(CompilationUnitSyntax syntax, InvocationExpressionSyntax beginXxxCall, SemanticModel model, Workspace workspace, ArgumentSyntax callbackArgument)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");
            if (beginXxxCall == null) throw new ArgumentNullException("beginXxxCall");
            if (model == null) throw new ArgumentNullException("model");

            const string taskName = "task";

            var lambda = (SimpleLambdaExpressionSyntax)callbackArgument.Expression;

            if (lambda.Body.Kind != SyntaxKind.Block)
                throw new NotImplementedException("Lambda body must be rewritten as BlockSyntax - it is now: " + lambda.Body.Kind + ": lambda: " + lambda);

            var lambdaBlock = (BlockSyntax)lambda.Body;

            var stateArgument = FindInvocationArgument(model, beginXxxCall, "object");
            if (stateArgument.Expression.Kind != SyntaxKind.NullLiteralExpression)
                throw new NotImplementedException("APM Begin method invocation `state' argument must be null - it is now: " + stateArgument.Expression.Kind + ": " + stateArgument);

            // TODO: Look up the symbol to check that it actually exists.
            var methodNameBase = GetAsyncMethodNameBase(beginXxxCall);

            var endStatement = TryFindEndXxxCallSyntaxNode(lambdaBlock, methodNameBase);

            if (endStatement != null)
            {
                return RewriteNotNestedInstance(syntax, beginXxxCall, lambdaBlock, endStatement, methodNameBase, workspace);
            }

            // Every method invocation might lead to the target EndXxx. Try to find it recursively.
            // Once found, rewrite the methods in the invocation path, one by one.
            // Finally, rewrite the originating method, and the method with the EndXxx statement.

            var invocationPathToEndXxx = TryFindCallGraphPathToEndXxx(lambdaBlock, methodNameBase, model);

            // These two get special treatment.
            var initialCall = invocationPathToEndXxx.RemoveLast();
            var endXxxCall = invocationPathToEndXxx.RemoveFirst();

            MethodSymbol endXxxMethod;
            try
            {
                endXxxMethod = model.LookupMethodSymbol(endXxxCall);
            }
            catch (SymbolMissingException e)
            {
                Logger.Error("No symbol found for APM End invocation: {0}", endXxxCall, e);

                throw new RefactoringException("No symbol found for APM End invocation: " + endXxxCall, e);
            }

            var taskTypeParameter = endXxxMethod.ReturnType.Name;

            var replacements = new List<SyntaxReplacementPair>(invocationPathToEndXxx.Count + 2);

            // Replace all intermediate methods on the call graph path.
            replacements.AddRange(
                invocationPathToEndXxx.Select(
                    invocation => new SyntaxReplacementPair(
                        invocation.ContainingMethod(),
                        RewriteCallGraphPathComponent(invocation, taskTypeParameter)
                    )
                )
            );

            // Replace method that contains the BeginXxx call.
            replacements.Add(
                new SyntaxReplacementPair(
                    beginXxxCall.ContainingMethod(),
                    RewriteOriginatingMethod(
                        beginXxxCall,
                        RewriteOriginatingMethodLambdaBlock(lambda, initialCall, taskName),
                        methodNameBase
                    )
                )
            );

            // Replace method that contains the EndXxx call.
            replacements.Add(
                new SyntaxReplacementPair(
                    endXxxCall.ContainingMethod(),
                    RewriteEndXxxContainingMethod(
                        endXxxCall,
                        taskTypeParameter
                    )
                )
            );

            return syntax.ReplaceAll(replacements)
                         .Format(workspace);
        }

        private static CompilationUnitSyntax RewriteNotNestedInstance(CompilationUnitSyntax syntax, InvocationExpressionSyntax beginXxxCall, BlockSyntax lambdaBlock, InvocationExpressionSyntax endStatement, string methodNameBase, Workspace workspace)
        {
            var awaitStatement = NewAwaitExpression("task");
            var rewrittenLambdaBlock = lambdaBlock.ReplaceNode(endStatement, awaitStatement);

            var newCallingMethod = RewriteOriginatingMethod(beginXxxCall, rewrittenLambdaBlock, methodNameBase);
            var originalCallingMethod = beginXxxCall.ContainingMethod();

            return syntax.ReplaceNode(originalCallingMethod, newCallingMethod)
                         .Format(workspace);
        }

        private static MethodDeclarationSyntax RewriteOriginatingMethod(InvocationExpressionSyntax beginXxxCall, BlockSyntax rewrittenLambdaBlock, string methodNameBase)
        {
            // TODO: beginXxxCall.Expression does not have to be a MemberAccessExpression.
            var tapStatement = NewVariableDeclarationStatement("task", ((MemberAccessExpressionSyntax)beginXxxCall.Expression).Expression.ToString(), methodNameBase + "Async");
            var endXxxStatement = beginXxxCall.ContainingStatement();

            var originalCallingMethod = beginXxxCall.ContainingMethod();

            var rewrittenMethod = originalCallingMethod
                .ReplaceNode(
                    endXxxStatement,
                    tapStatement
                )
                .AddBodyStatements(rewrittenLambdaBlock.Statements.ToArray());

            if (!rewrittenMethod.HasAsyncModifier())
                rewrittenMethod = rewrittenMethod.AddModifiers(NewAsyncKeyword());

            return rewrittenMethod;
        }

        /// <summary>
        /// Rewrite the originating method's lambda expression block so that its statements can be 'concatenated' to the originating method.
        /// </summary>
        /// <param name="lambda">The SimpleLambdaExpressionSyntax which must be rewritten.</param>
        /// <param name="callOnPathToEndXxxCall">The InvocationExpressionSyntax that represents the invocation of the callback in the lambda expression.</param>
        /// <param name="taskName">The name of the Task object that must be provided to the callback.</param>
        /// <returns>A rewritten BlockSyntax whose statements can be added to the originating method.</returns>
        private static BlockSyntax RewriteOriginatingMethodLambdaBlock(SimpleLambdaExpressionSyntax lambda, InvocationExpressionSyntax callOnPathToEndXxxCall, string taskName)
        {
            if (lambda == null) throw new ArgumentNullException("lambda");
            if (callOnPathToEndXxxCall == null) throw new ArgumentNullException("callOnPathToEndXxxCall");
            if (taskName == null) throw new ArgumentNullException("taskName");

            var asyncResultRefArg = callOnPathToEndXxxCall.ArgumentList.Arguments
                .First(arg => ((IdentifierNameSyntax)arg.Expression).Identifier.ValueText.Equals(lambda.Parameter.Identifier.ValueText));

            var awaitStatement = NewAwaitExpression(
                callOnPathToEndXxxCall.ReplaceNode(
                    asyncResultRefArg,
                    SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(
                            taskName
                        )
                    )
                )
            );

            return ((BlockSyntax)lambda.Body).ReplaceNode(
                callOnPathToEndXxxCall,
                awaitStatement
            );
        }

        private static MethodDeclarationSyntax RewriteEndXxxContainingMethod(InvocationExpressionSyntax endXxxCall, string taskType)
        {
            const string taskName = "task";

            var originalMethod = endXxxCall.ContainingMethod();
            var returnType = NewTaskifiedReturnType(originalMethod);

            var replacements = new List<SyntaxReplacementPair>();

            var asyncResultParameter = FindIAsyncResultParameter(originalMethod.ParameterList);
            var taskParameter = NewGenericTaskParameter(taskName, taskType);
            replacements.Add(new SyntaxReplacementPair(asyncResultParameter, taskParameter));

            replacements.Add(new SyntaxReplacementPair(
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

        private static MethodDeclarationSyntax RewriteCallGraphPathComponent(InvocationExpressionSyntax invocation, String taskType)
        {
            if (invocation == null) throw new ArgumentNullException("invocation");

            const string taskName = "task";

            var method = invocation.ContainingMethod();

            var asyncResultParam = FindIAsyncResultParameter(method.ParameterList);

            var returnType = NewGenericTask(method.ReturnType);

            var taskParam = NewGenericTaskParameter(taskName, taskType);
            var parameterList = method.ParameterList.ReplaceNode(asyncResultParam, taskParam);

            var taskRef = SyntaxFactory.IdentifierName(taskName);

            var replacements = method.Body.DescendantNodes()
                                                        .OfType<IdentifierNameSyntax>()
                                                        .Where(id => id.Identifier.ValueText.Equals(asyncResultParam.Identifier.ValueText))
                                                        .Select(asyncResultRef => new SyntaxReplacementPair(asyncResultRef, taskRef))
                                                        .ToList();
            replacements.Add(AwaitedReplacementForCallGraphComponentInvocation(invocation, asyncResultParam, taskRef));

            var body = method.Body.ReplaceAll(replacements);

            return method.AddModifiers(NewAsyncKeyword())
                         .WithReturnType(returnType)
                         .WithParameterList(parameterList)
                         .WithBody(body);
        }

        private static SyntaxReplacementPair AwaitedReplacementForCallGraphComponentInvocation(InvocationExpressionSyntax invocation, ParameterSyntax asyncResultParam, IdentifierNameSyntax taskRef)
        {
            var invocationAsyncResultRef = invocation.DescendantNodes()
                                                     .OfType<IdentifierNameSyntax>()
                                                     .First(id => id.Identifier.ValueText.Equals(asyncResultParam.Identifier.ValueText));

            var awaitReplacement = new SyntaxReplacementPair(
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
                MethodSymbol methodSymbol;
                try
                {
                    methodSymbol = model.LookupMethodSymbol(candidate);
                }
                catch (SymbolMissingException)
                {
                    Logger.Trace("Symbol missing for candidate: {0} - ignoring ...", candidate);
                    continue;
                }

                var methodSyntax = methodSymbol.FindMethodDeclarationNode();
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

        private static string GetAsyncMethodNameBase(InvocationExpressionSyntax invocation)
        {
            if (invocation == null) throw new ArgumentNullException("invocation");

            var expression = (MemberAccessExpressionSyntax)invocation.Expression;

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
        /// <returns>The MethodDeclarationSyntax node of the method that contains the given syntax node, or null if it is not contained in a method.</returns>
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

        /// <summary>
        /// Returns the StatementSyntax containing this node.
        /// </summary>
        /// This node is supposedly contained in a StatementSyntax node.
        /// That StatementSyntax (or subclass) instance is returned.
        ///
        /// TODO: This method does not consider e.g. lambda expressions.
        ///
        /// <param name="node">The SyntaxNode of which the parent must be looked up.</param>
        /// <returns>The containing StatementSyntax node, or null if it is not contained in a statement.</returns>
        public static StatementSyntax ContainingStatement(this SyntaxNode node)
        {
            if (node == null) throw new ArgumentNullException("node");

            var parent = node.Parent;

            while (parent != null)
            {
                var syntax = parent as StatementSyntax;
                if (syntax != null)
                {
                    return syntax;
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

            MethodSymbol symbol;
            try
            {
                symbol = model.LookupMethodSymbol(invocation);
            }
            catch (SymbolMissingException e)
            {
                Logger.Trace("No symbol found for invocation: {0}", invocation, e);
                throw new ArgumentException("No symbol found for invocation: " + invocation, e);
            }

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
            var expression = SyntaxFactory.ParseExpression(code);

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

            var code = String.Format(@"await {0}.ConfigureAwait(false)", expression);

            return SyntaxFactory.ParseExpression(code);
        }

        private static ExpressionSyntax NewAwaitExpression(string taskName)
        {
            if (taskName == null) throw new ArgumentNullException("taskName");

            var code = String.Format(@"await {0}.ConfigureAwait(false)", taskName);

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

            return SyntaxFactory.Parameter(
                SyntaxFactory.Identifier(name)
            );
        }

        private static TypeSyntax NewTaskifiedReturnType(MethodDeclarationSyntax originalMethod)
        {
            if (originalMethod == null) throw new ArgumentNullException("originalMethod");

            return originalMethod.ReturnsVoid()
                ? SyntaxFactory.ParseTypeName("Task")
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
                SyntaxFactory.Identifier(taskName)
            );
        }

        private static ParameterSyntax NewParameter(TypeSyntax type, SyntaxToken name)
        {
            return SyntaxFactory.Parameter(
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(),
                type,
                name,
                null
            );
        }

        private static TypeSyntax NewGenericTask(string parameterType)
        {
            if (parameterType == null) throw new ArgumentNullException("parameterType");

            return NewGenericTask(
                SyntaxFactory.ParseTypeName(parameterType)
            );
        }

        private static TypeSyntax NewGenericTask(TypeSyntax parameter)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");

            var identifier = SyntaxFactory.Identifier("Task");

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

            return SyntaxFactory.Block(
                SyntaxFactory.List(
                    statements
                )
            );
        }

        private static ArgumentListSyntax NewSingletonArgumentList(ExpressionSyntax expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            return SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    SyntaxFactory.Argument(
                        expression
                    )
                )
            );
        }

        private static ArgumentListSyntax NewArgumentList(params ExpressionSyntax[] expressions)
        {
            if (expressions == null) throw new ArgumentNullException("expressions");

            return SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    expressions.Select(
                        SyntaxFactory.Argument
                    )
                )
            );
        }

        private static ExpressionStatementSyntax NewInvocationStatement(ExpressionSyntax expression, ArgumentListSyntax argumentList)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (argumentList == null) throw new ArgumentNullException("argumentList");

            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    expression,
                    argumentList
                )
            );
        }

        private static T RemoveFirst<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException("list");

            var element = list.ElementAt(0);

            list.RemoveAt(0);

            return element;
        }

        private static T RemoveLast<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException("list");

            var element = list.Last();

            list.RemoveAt(list.Count - 1);

            return element;
        }

        public static int GetNumRefactoringCandidatesInDocument(this Document document)
        {
            return document.GetSyntaxRootAsync().Result
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Count(node => node.HasAnnotations<RefactorableAPMInstance>());
        }

        public static InvocationExpressionSyntax GetAnnotatedInvocation(this Document document, int index)
        {
            return document.GetSyntaxRootAsync().Result
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .First(
                    node => node
                        .GetAnnotations<RefactorableAPMInstance>()
                        .Any(annotation => annotation.Index == index)
                );
        }

        public static int GetStartLineNumber(this SyntaxNode node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (node.SyntaxTree == null) throw new ArgumentException("node.SyntaxTree is null");

            return node.SyntaxTree.GetLineSpan(node.Span, false).StartLinePosition.Line;
        }
    }

    public class RefactoringException : Exception
    {
        public RefactoringException(string message)
            : base(message)
        {
        }

        public RefactoringException(string message, SymbolMissingException innerException)
            : base(message, innerException)
        {
        }

        public RefactoringException(string message, SyntaxNode node)
            : base(message + ": " + node.SyntaxTree.FilePath + ":" + node.GetStartLineNumber())
        {
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Refactoring.RefactoringExtensions;

namespace Refactoring
{
    public static class Taskifier
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string DefaultTaskName = "task";
        private const string DefaultLambdaParamName = "result";

        /// <summary>
        /// Execute the APM-to-async/await refactoring for a given APM method invocation.
        /// </summary>
        /// <param name="document">The C# Document on which to operate/in which the Begin and End method calls are represented.</param>
        /// <param name="solution">The solution that contains the C# document.</param>
        /// <param name="workspace">The workspace to which the code in the syntax tree currently belongs, for formatting purposes.</param>
        /// <param name="index">The index number </param>
        /// <returns>The CompilationUnitSyntax node that is the result of the transformation.</returns>
        public static CompilationUnitSyntax RefactorToTask(Document document, Solution solution, Workspace workspace, int index)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (workspace == null) throw new ArgumentNullException("workspace");

            String message;

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
            switch (callbackExpression.CSharpKind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    var lambda = (SimpleLambdaExpressionSyntax)callbackExpression;

                    switch (lambda.Body.CSharpKind())
                    {
                        case SyntaxKind.Block:
                            var stateArgument = FindAsyncStateInvocationArgument(model, beginXxxCall);

                            switch (stateArgument.Expression.CSharpKind())
                            {
                                case SyntaxKind.NullLiteralExpression:
                                    Logger.Info("Refactoring:\n{0}", beginXxxCall.ContainingMethod());

                                    return RefactorSimpleLambdaInstance(syntax, beginXxxCall, model, workspace, callbackArgument);

                                default:
                                    Logger.Info("Rewriting to remove state argument:\n{0}", beginXxxCall);

                                    rewrittenSyntax = RewriteStateArgumentToNull(lambda, syntax, stateArgument);

                                    break;
                            }

                            break;

                        case SyntaxKind.InvocationExpression:
                            Logger.Info("Rewriting lambda to block form:\n{0}", beginXxxCall);

                            rewrittenSyntax = RewriteInvocationExpressionToBlock(syntax, lambda, model, beginXxxCall);
                            break;

                        default:
                            message = String
                                .Format(
                                    "Unsupported lambda body kind: {0}: method:\n{1}",
                                    lambda.Body.CSharpKind(),
                                    beginXxxCall.ContainingMethod()
                                );

                            Logger.Error("Not implemented: {0}", message);

                            throw new NotImplementedException(message);
                    }

                    break;

                case SyntaxKind.IdentifierName:
                case SyntaxKind.SimpleMemberAccessExpression:
                    Logger.Info("Rewriting method reference to lambda:\n{0}", beginXxxCall);

                    rewrittenSyntax = RewriteMethodReferenceToSimpleLambda(syntax, beginXxxCall, model, callbackArgument, callbackExpression);
                    break;

                case SyntaxKind.ParenthesizedLambdaExpression:
                    Logger.Info("Rewriting parenthesized lambda to simple lambda:\n{0}", beginXxxCall);

                    rewrittenSyntax = RewriteParenthesizedLambdaToSimpleLambda(syntax, beginXxxCall, model);
                    break;

                case SyntaxKind.ObjectCreationExpression:
                    Logger.Info("Rewriting object creation expression to simple lambda:\n{0}", beginXxxCall);

                    var objectCreation = (ObjectCreationExpressionSyntax)callbackExpression;

                    rewrittenSyntax = RewriteObjectCreationToSimpleLambda(syntax, objectCreation, workspace);
                    break;

                case SyntaxKind.AnonymousMethodExpression:
                    Logger.Info("Rewriting anonymous method (delegate) expression to simple lambda:\n{0}", beginXxxCall);

                    var anonymousMethod = (AnonymousMethodExpressionSyntax)callbackExpression;

                    rewrittenSyntax = RewriteAnonymousMethodToSimpleLambda(syntax, anonymousMethod, workspace);
                    break;

                case SyntaxKind.NullLiteralExpression:
                    message = String.Format("callback is null:\n{0}", beginXxxCall.ContainingMethod());

                    Logger.Error("Precondition failed: {0}", message);

                    throw new PreconditionException(message);

                case SyntaxKind.InvocationExpression:
                    message = String
                        .Format(
                            "InvocationExpression as callback is not supported: {0}",
                            beginXxxCall
                        );

                    Logger.Error("Precondition failed: {0}", message);

                    throw new PreconditionException(message);

                case SyntaxKind.GenericName:
                    message = String.Format("GenericName syntax kind is not supported");

                    Logger.Error("Precondition failed: {0}", message);

                    throw new PreconditionException(message);

                default:
                    message = String.Format(
                        "Unsupported actual argument syntax node kind: {0}: callback argument: {1}: in method:\n{2}",
                        callbackExpression.CSharpKind(),
                        callbackArgument,
                        beginXxxCall.ContainingMethod()
                    );

                    Logger.Error(message);

                    throw new NotImplementedException(message);
            }

            var rewrittenDocument = document.WithSyntaxRoot(rewrittenSyntax);
            var rewrittenSolution = solution.WithDocumentSyntaxRoot(document.Id, rewrittenSyntax);

            //if (rewrittenSolution.CompilationErrorCount() > numErrorsInSolutionBeforeRewriting)
            //{
            //    Logger.Error(
            //        "Rewritten solution contains more compilation errors than the original solution while refactoring: {0} @ {1}:{2} in method:\n{3}",
            //        beginXxxCall,
            //        beginXxxCall.SyntaxTree.FilePath,
            //        beginXxxCall.GetStartLineNumber(),
            //        beginXxxCall.ContainingMethod()
            //    );

            //    Logger.Warn("=== SOLUTION ERRORS ===");
            //    foreach (var diagnostic in rewrittenSolution.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
            //    {
            //        Logger.Warn("  - {0}", diagnostic);
            //    }
            //    Logger.Warn("=== END OF SOLUTION ERRORS ===");

            //    Logger.Warn("\n### ORIGINAL CODE ###\n{0}### END OF CODE ###", syntax.Format(workspace));
            //    Logger.Warn("\n### REWRITTEN CODE ###\n{0}### END OF CODE ###", rewrittenSyntax.Format(workspace));

            //    throw new RefactoringException("Rewritten solution contains more compilation errors than the original refactoring");
            //}

            return null;
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
}

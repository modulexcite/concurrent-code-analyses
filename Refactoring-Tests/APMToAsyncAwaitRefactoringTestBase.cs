using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using NUnit.Framework;
using Refactoring;
using System;
using Utilities;

namespace Refactoring_Tests
{
    /// <summary>
    /// Base class for APM-to-async/await refactoring testing.
    /// For clarity, use a single test class per test case.
    /// </summary>
    public class APMToAsyncAwaitRefactoringTestBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Find the invocation expression statement representing an APM BeginXxx method call that must be refactored in the given compilation unit.
        /// </summary>
        /// <param name="syntaxTree">The SyntaxTree in which the invocation expression must be found.</param>
        /// <returns>The invocation expression statement.</returns>
        public delegate InvocationExpressionSyntax StatementFinder(SyntaxTree syntaxTree);

        /// <summary>
        /// Assert that given original code containing both the BeginXxx method
        /// call, callback method declaration and EndXxx method call, is
        /// correctly refactored to the given refactored code statement. The
        /// statementFinder is used to return the APM invocation expression
        /// statement that must be refactored.
        /// </summary>
        /// <param name="originalCode">The original code to refactor.</param>
        /// <param name="refactoredCode">The refactored code to check against.</param>
        /// <param name="statementFinder">The delegate that returns the APM BeginXxx
        /// invocation expression statement that must be refactored.</param>
        protected static void AssertThatOriginalCodeIsRefactoredCorrectly(string originalCode, string refactoredCode, StatementFinder statementFinder)
        {
            Logger.Debug("=== CODE TO BE REFACTORED ===\n{0}\n=== END OF CODE ===", originalCode);

            // Parse given original code
            var originalSyntaxTree = SyntaxTree.ParseText(originalCode);

            // Replace invocation of interest with annotated version.
            var originalApmInvocation = statementFinder(originalSyntaxTree);
            var annotatedApmInvocation = (SyntaxNode)originalApmInvocation.WithAdditionalAnnotations(new RefactorableAPMInstance());
            var annotatedSyntax = ((CompilationUnitSyntax)originalSyntaxTree.GetRoot()).ReplaceNode(originalApmInvocation, annotatedApmInvocation);
            var annotatedTree = SyntaxTree.Create(annotatedSyntax);

            Logger.Trace("Invocation tagged for refactoring: {0}", annotatedApmInvocation);

            // Parse given refactored code
            var refactoredSyntaxTree = SyntaxTree.ParseText(refactoredCode);
            var refactoredSyntax = refactoredSyntaxTree.GetCompilationUnitRoot();

            var workspace = new CustomWorkspace();

            var actualRefactoredSyntax = PerformRefactoring(annotatedTree, workspace);

            Logger.Debug("=== REFACTORED CODE ===\n{0}\n=== END OF CODE ===", actualRefactoredSyntax.Format(workspace));

            // Test against refactored code
            // TODO: The first assertion seems to regard \r\n as different from \n.
            //Assert.That(actualRefactoredSyntax, Is.EqualTo(refactoredSyntax));
            Assert.That(actualRefactoredSyntax.ToString().Replace("\r\n", "\n"), Is.EqualTo(refactoredSyntax.ToString().Replace("\r\n", "\n")));
        }

        private static CompilationUnitSyntax PerformRefactoring(SyntaxTree originalSyntax, Workspace workspace)
        {
            Logger.Trace("Starting refactoring operation ...");
            var start = DateTime.UtcNow;

            // Perform actual refactoring
            var actualRefactoredSyntax = originalSyntax.RefactorAPMToAsyncAwait(workspace);

            var end = DateTime.UtcNow;
            var time = end.Subtract(start).Milliseconds;
            Logger.Trace("Finished refactoring operation in {0} ms", time);

            return actualRefactoredSyntax;
        }
    }
}

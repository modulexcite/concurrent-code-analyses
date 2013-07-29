using System;
using NUnit.Framework;
using Refactoring;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Refactoring_Tests
{
    /// <summary>
    /// Base class for APM-to-async/await refactoring testing.
    /// For clarity, use a single test class per test case.
    /// </summary>
    public class APMToAsyncAwaitRefactoringTestBase
    {
        // ReSharper disable InconsistentNaming
        private static readonly MetadataReference mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");

        private static readonly MetadataReference system = MetadataReference.CreateAssemblyReference("system");
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Find the invocation expression statement of the APM BeginXxx method call that must be refactored in the given compilation unit.
        /// </summary>
        /// <param name="syntax">The compilation unit in which the invocation expression must be found.</param>
        /// <returns>The invocation expression statement.</returns>
        public delegate ExpressionStatementSyntax StatementFinder(CompilationUnitSyntax syntax);

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
            Console.WriteLine("=== CODE TO BE REFACTORED ===\n{0}\n=== END ===", originalCode);

            // Parse given original code
            var originalSyntaxTree = SyntaxTree.ParseText(originalCode);
            var originalSyntax = originalSyntaxTree.GetRoot();
            var originalCompilation = Compilation.Create(
                "OriginalCompilation",
                syntaxTrees: new[] { originalSyntaxTree },
                references: new[] { mscorlib, system }
                );
            var originalSemanticModel = originalCompilation.GetSemanticModel(originalSyntaxTree);

            var apmInvocation = statementFinder(originalSyntax);

            // Parse given refactored code
            var refactoredSyntaxTree = SyntaxTree.ParseText(refactoredCode);
            var refactoredSyntax = refactoredSyntaxTree.GetRoot();

            var actualRefactoredSyntax = PerformRefactoring(originalSyntax, apmInvocation, originalSemanticModel);

            // Test against refactored code
            Assert.That(actualRefactoredSyntax, Is.EqualTo(refactoredSyntax));
            Assert.That(actualRefactoredSyntax.ToString(), Is.EqualTo(refactoredSyntax.ToString()));
        }

        private static CompilationUnitSyntax PerformRefactoring(CompilationUnitSyntax originalSyntax, ExpressionStatementSyntax apmInvocation, SemanticModel originalSemanticModel)
        {

            Console.WriteLine("Starting refactoring operation ...");
            var start = DateTime.UtcNow;

            // Perform actual refactoring
            var actualRefactoredSyntax = originalSyntax.RefactorAPMToAsyncAwait(apmInvocation, originalSemanticModel);

            var end = DateTime.UtcNow;
            var time = end.Subtract(start).Milliseconds;
            Console.WriteLine("Finished refactoring operation in {0} ms", time);

            return actualRefactoredSyntax;
        }
    }
}

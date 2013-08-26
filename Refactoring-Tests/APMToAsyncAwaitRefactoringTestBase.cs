using System.Linq;
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

        // ReSharper disable InconsistentNaming
        private static readonly MetadataReference mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");
        private static readonly MetadataReference system = MetadataReference.CreateAssemblyReference("System");
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Find the invocation expression statement representing an APM BeginXxx method call that must be refactored in the given compilation unit.
        /// </summary>
        /// <param name="syntaxTree">The SyntaxTree in which the invocation expression must be found.</param>
        /// <returns>The invocation expression statement.</returns>
        public delegate InvocationExpressionSyntax InvocationExpressionFinder(SyntaxTree syntaxTree);

        /// <summary>
        /// Assert that given original code containing both the BeginXxx method
        /// call, callback method declaration and EndXxx method call, is
        /// correctly refactored to the given refactored code statement. The
        /// statementFinder is used to return the APM invocation expression
        /// statement that must be refactored.
        /// </summary>
        /// <param name="originalCode">The original code to refactor.</param>
        /// <param name="refactoredCode">The refactored code to check against.</param>
        /// <param name="invocationExpressionFinders">One or more delegates that returns the APM BeginXxx
        /// invocation expression statement(s) that must be refactored.</param>
        protected static void AssertThatOriginalCodeIsRefactoredCorrectly(string originalCode, string refactoredCode, params InvocationExpressionFinder[] invocationExpressionFinders)
        {
            if (originalCode == null) throw new ArgumentNullException("originalCode");
            if (refactoredCode == null) throw new ArgumentNullException("refactoredCode");
            if (invocationExpressionFinders == null) throw new ArgumentNullException("invocationExpressionFinders");

            if (invocationExpressionFinders.Length == 0) throw new ArgumentException("Must provide at least one StatementFinder");

            Logger.Debug("\n=== CODE TO BE REFACTORED ===\n{0}\n=== END OF CODE ===", originalCode);

            var workspace = new CustomWorkspace();

            var originalDocument = CreateOriginalDocument(originalCode, workspace);

            var annotatedDocument = AnnotateDocument(originalDocument, invocationExpressionFinders);

            var refactoredSyntax = RefactorDocument(annotatedDocument, invocationExpressionFinders.Length, workspace);

            var expectedSyntaxTree = SyntaxTree.ParseText(refactoredCode);
            var expectedSyntax = expectedSyntaxTree.GetCompilationUnitRoot();

            Assert.IsNotNull(refactoredSyntax);
            Assert.That(refactoredSyntax.ToString().Replace("\r\n", "\n"), Is.EqualTo(expectedSyntax.ToString().Replace("\r\n", "\n")));
        }

        private static Document AnnotateDocument(Document document, params InvocationExpressionFinder[] invocationExpressionFinders)
        {
            for (var index = 0; index < invocationExpressionFinders.Length; index++)
            {
                document = AnnotateInvocation(document, invocationExpressionFinders.ElementAt(index), index);
            }

            return document;
        }

        private static Document CreateOriginalDocument(string originalCode, CustomWorkspace workspace)
        {
            var projectId = workspace.AddProject("ProjectUnderTest", LanguageNames.CSharp);
            var documentId = workspace.AddDocument(projectId, "SourceUnderTest.cs", originalCode);

            var originalSolution = workspace.CurrentSolution
                .AddMetadataReference(projectId, mscorlib)
                .AddMetadataReference(projectId, system);

            return originalSolution.GetDocument(documentId);
        }

        private static CompilationUnitSyntax RefactorDocument(Document document, int numInstances, Workspace workspace)
        {
            CompilationUnitSyntax refactoredSyntax = null;

            for (var index = 0; index < numInstances; index++)
            {
                refactoredSyntax = PerformRefactoring(document, workspace, index);
                document = document.WithSyntaxRoot(refactoredSyntax);
            }

            return refactoredSyntax;
        }

        private static CompilationUnitSyntax PerformRefactoring(Document document, Workspace workspace, int index)
        {
            var refactoredSyntax = PerformTimedRefactoring(document, workspace, index);

            Logger.Debug("=== REFACTORED CODE ===\n{0}\n=== END OF CODE ===", refactoredSyntax.Format(workspace));

            return refactoredSyntax;
        }

        private static Document AnnotateInvocation(Document document, InvocationExpressionFinder invocationExpressionFinder, int index)
        {
            var originalSyntaxTree = (SyntaxTree)document.GetSyntaxTreeAsync().Result;
            var originalApmInvocation = invocationExpressionFinder(originalSyntaxTree);

            var annotatedApmInvocation = originalApmInvocation
                .WithAdditionalAnnotations(
                    new RefactorableAPMInstance(index)
                );

            var annotatedSyntax = ((CompilationUnitSyntax)originalSyntaxTree.GetRoot())
                .ReplaceNode(
                    originalApmInvocation,
                    annotatedApmInvocation
                );

            Logger.Trace("Invocation tagged for refactoring: {0}", annotatedApmInvocation);

            return document.WithSyntaxRoot(annotatedSyntax);
        }

        private static CompilationUnitSyntax PerformTimedRefactoring(Document document, Workspace workspace, int index)
        {
            Logger.Trace("Starting refactoring operation ...");
            var start = DateTime.UtcNow;

            var actualRefactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(document, workspace, index);

            var end = DateTime.UtcNow;
            var time = end.Subtract(start).Milliseconds;
            Logger.Trace("Finished refactoring operation in {0} ms", time);

            return actualRefactoredSyntax;
        }

        protected internal static InvocationExpressionFinder FirstBeginInvocationFinder(string nodeText)
        {
            return tree => tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .First(node => node.ToString().Contains(nodeText));
        }
    }
}

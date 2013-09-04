using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Refactoring;
using Utilities;

namespace Refactoring_BatchTool
{
    public class SolutionRefactoring
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Workspace _workspace;

        private readonly Solution _originalSolution;
        private Solution _refactoredSolution;

        public int NumCandidates { get; private set; }
        public int NumRefactoringExceptions { get; private set; }
        public int NumNotImplementedExceptions { get; private set; }
        public int NumOtherExceptions { get; private set; }

        public int NumFailedRefactorings
        {
            get { return NumRefactoringExceptions + NumNotImplementedExceptions + NumOtherExceptions; }
        }

        public int NumSuccesfulRefactorings
        {
            get { return NumCandidates - NumFailedRefactorings; }
        }

        public SolutionRefactoring(Workspace workspace)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");

            _workspace = workspace;

            _originalSolution = _workspace.CurrentSolution;
        }

        public void Run()
        {
            var documents = _originalSolution.Projects
                .Where(project => project.IsWindowsPhoneProject() > 0)
                .SelectMany(project => project.Documents)
                .Where(document => document.FilePath.EndsWith(".cs")) // Only cs files.
                .Where(document => !document.FilePath.EndsWith("Test.cs")); // No tests.

            _refactoredSolution = documents.Aggregate(_originalSolution,
                (sln, doc) => CheckDocument(doc, sln));

            Logger.Info("Applying changes to workspace ...");
            if (!_workspace.TryApplyChanges(_refactoredSolution))
            {
                Logger.Error("Failed to apply changes in solution to workspace");
            }
        }

        private Solution CheckDocument(Document document, Solution solution)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (solution == null) throw new ArgumentNullException("solution");

            Logger.Trace("Checking document: {0}", document.FilePath);

            var annotatedSolution = AnnotateDocumentInSolution(document, solution);

            return RefactorDocumentInSolution(document, annotatedSolution);
        }

        private Solution AnnotateDocumentInSolution(Document document, Solution solution)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (solution == null) throw new ArgumentNullException("solution");

            var annotater = new APMBeginInvocationAnnotater();
            var annotatedDocument = annotater.Annotate(document);

            NumCandidates += annotater.NumAnnotations;

            return solution.WithDocumentSyntaxRoot(
                document.Id,
                annotatedDocument.GetSyntaxRootAsync().Result
            );
        }

        private Solution RefactorDocumentInSolution(Document document, Solution solution)
        {
            var refactoredSolution = solution;
            var refactoredDocument = refactoredSolution.GetDocument(document.Id);
            var candidatesInDocument = refactoredDocument.GetNumRefactoringCandidatesInDocument();

            Logger.Trace("Found {0} APM instances. Refactoring one-by-one ...", candidatesInDocument);
            for (var index = 0; index < candidatesInDocument; index++)
            {
                var beginXxxSyntax = refactoredDocument.GetAnnotatedInvocation(index);

                // TODO: In the LocalDeclarationStatementSyntax case, the declared variable must be checked for non-use.
                if (beginXxxSyntax.ContainingStatement() is ExpressionStatementSyntax ||
                    beginXxxSyntax.ContainingStatement() is LocalDeclarationStatementSyntax)
                {
                    refactoredSolution = SafelyRefactorSolution(refactoredSolution, refactoredDocument, index);
                    refactoredDocument = refactoredSolution.GetDocument(document.Id);
                }
                else
                {
                    Logger.Warn(
                        "APM Begin invocation containing statement is not yet supported: index={0}: {1}: statement: {2}",
                        index,
                        beginXxxSyntax.ContainingStatement().Kind,
                        beginXxxSyntax.ContainingStatement()
                    );
                }
            }

            return refactoredSolution;
        }

        private Solution SafelyRefactorSolution(Solution solution, Document document, int index)
        {
            var numErrors = solution.CompilationErrorCount();

            var oldSolution = solution;
            try
            {
                solution = ExecuteRefactoring(document, solution, index);

                if (solution.CompilationErrorCount() > numErrors)
                {
                    Logger.Error("Refactoring {0} caused new compilation errors. It will not be applied.", index);
                    solution = oldSolution;
                }
            }
            catch (RefactoringException e)
            {
                Logger.Error("Refactoring failed: index={0}: {1}: {2}", index, e.Message, e);
                solution = oldSolution;

                NumRefactoringExceptions++;
            }
            catch (NotImplementedException e)
            {
                Logger.Error("Not implemented: index={0}: {1}: {2}", index, e.Message, e);
                solution = oldSolution;

                NumNotImplementedExceptions++;
            }
            catch (Exception e)
            {
                Logger.Error("Unhandled exception while refactoring: index={0}: {1}\n{2}", index, e.Message, e);
                solution = oldSolution;

                NumOtherExceptions++;
            }

            return solution;
        }

        private Solution ExecuteRefactoring(Document document, Solution solution, int index)
        {
            if (document == null) throw new ArgumentNullException("document");

            var syntax = ((SyntaxTree)document.GetSyntaxTreeAsync().Result).GetRoot();

            Logger.Debug("Refactoring annotated document: index={0}", index);
            Logger.Debug("=== CODE TO REFACTOR ===\n{0}=== END OF CODE ===", syntax);

            var startTime = DateTime.UtcNow;

            var refactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(document, solution, _workspace, index);

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Debug("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Debug("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredSyntax.Format(_workspace));

            return solution.WithDocumentSyntaxRoot(document.Id, refactoredSyntax);
        }
    }
}

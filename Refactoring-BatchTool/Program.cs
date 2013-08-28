using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Utilities;

namespace Refactoring_BatchTool
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\Mono.Data.Sqlite\Mono.Data.Sqlite.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\Weather\Weather.sln";

        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\topaz-fuel-card-windows-phone\Topaz Fuel Card.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\WAZDash\WAZDash7.1.sln";

        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\awful2\wp\Awful\Awful.WP7.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\awful2\wp\Awful\Awful.WP8.sln";

        private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\8digits-WindowsPhone-SDK-Sample-App\EightDigitsTest.sln";

        private static int _numCandidates;
        private static int _numRefactoringExceptions;
        private static int _numNotImplementedExceptions;
        private static int _numOtherExceptions;

        static void Main()
        {
            Logger.Info("Hello, world!");

            try
            {
                DoWork(SolutionFile);
            }
            catch (Exception e)
            {
                Logger.Error("%%% CRITICAL ERROR %%%");
                Logger.Error("%%% Caught unexpected exception during work on solution file: {0}", SolutionFile);
                Logger.Error("%%% Caught exception: {0}:\n{1}", e.Message, e);
            }

            var numFailedRefactorings = _numRefactoringExceptions + _numNotImplementedExceptions + _numOtherExceptions;
            var numSuccesfulRefactorings = _numCandidates - numFailedRefactorings;

            Logger.Info("!!! REFACTORING RESULTS !!!");
            Logger.Info("!!! * Total number of candidates for refactoring: {0}", _numCandidates);
            Logger.Info("!!! * Number of succesful refactorings          : {0}", numSuccesfulRefactorings);
            Logger.Info("!!! * Number of failed refactorings             : {0}", numFailedRefactorings);
            Logger.Info("!!!    - RefactoringExceptions   : {0}", _numRefactoringExceptions);
            Logger.Info("!!!    - NotImplementedExceptions: {0}", _numNotImplementedExceptions);
            Logger.Info("!!!    - Other exceptions        : {0}", _numOtherExceptions);
            Logger.Info("!!! END OF RESULTS !!!");

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static void DoWork(String solutionPath)
        {
            if (solutionPath == null) throw new ArgumentNullException("solutionPath");

            Logger.Trace("Loading solution file: {0}", solutionPath);

            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.TryLoadSolutionAsync(solutionPath).Result;

            if (solution == null)
            {
                Logger.Error("Failed to load solution file: {0}", solutionPath);
                return;
            }

            var documents = solution.Projects
                .Where(project => project.IsWindowsPhoneProject() > 0)
                .SelectMany(project => project.Documents)
                .Where(document => document.FilePath.EndsWith(".cs")) // Only cs files.
                .Where(document => !document.FilePath.EndsWith("Test.cs")); // No tests.

            solution = documents.Aggregate(solution,
                (sln, doc) => CheckDocument(doc, workspace, sln));

            Logger.Info("Applying changes to workspace ...");
            if (!workspace.TryApplyChanges(solution))
            {
                Logger.Error("Failed to apply changes in solution to workspace");
            }
        }

        private static Solution CheckDocument(Document document, Workspace workspace, Solution solution)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (solution == null) throw new ArgumentNullException("solution");

            Logger.Trace("Checking document: {0}", document.FilePath);

            var annotatedSolution = AnnotateDocumentInSolution(document, solution);

            return RefactorDocumentInSolution(document, workspace, annotatedSolution);
        }

        private static Solution RefactorDocumentInSolution(Document document, Workspace workspace, Solution solution)
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
                    refactoredSolution = SafelyRefactorSolution(workspace, refactoredSolution, refactoredDocument, index);
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

        public static int GetNumRefactoringCandidatesInDocument(this Document document)
        {
            return document.GetSyntaxRootAsync().Result
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Count(node => node.HasAnnotations<RefactorableAPMInstance>());
        }

        private static Solution AnnotateDocumentInSolution(Document document, Solution solution)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (solution == null) throw new ArgumentNullException("solution");

            var annotater = new APMBeginInvocationAnnotater();
            var annotatedDocument = annotater.Annotate(document);

            _numCandidates += annotater.NumAnnotations;

            return solution.WithDocumentSyntaxRoot(
                document.Id,
                annotatedDocument.GetSyntaxRootAsync().Result
            );
        }

        private static Solution SafelyRefactorSolution(Workspace workspace, Solution solution, Document document, int index)
        {
            var numErrors = solution.CompilationErrorCount();

            var oldSolution = solution;
            try
            {
                solution = ExecuteRefactoring(workspace, document, solution, index);

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

                _numRefactoringExceptions++;
            }
            catch (NotImplementedException e)
            {
                Logger.Error("Not implemented: index={0}: {0}: {1}", index, e.Message, e);
                solution = oldSolution;

                _numNotImplementedExceptions++;
            }
            catch (Exception e)
            {
                Logger.Error("Unhandled exception while refactoring: index={0}: {1}\n{2}", index, e.Message, e);
                solution = oldSolution;

                _numOtherExceptions++;
            }

            return solution;
        }

        private static Solution ExecuteRefactoring(Workspace workspace, Document document, Solution solution, int index)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (document == null) throw new ArgumentNullException("document");

            var syntax = ((SyntaxTree)document.GetSyntaxTreeAsync().Result).GetRoot();

            Logger.Debug("Refactoring annotated document: index={0}", index);
            Logger.Debug("=== CODE TO REFACTOR ===\n{0}=== END OF CODE ===", syntax);

            var startTime = DateTime.UtcNow;

            var refactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(document, solution, workspace, index);

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Debug("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Debug("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredSyntax.Format(workspace));

            return solution.WithDocumentSyntaxRoot(document.Id, refactoredSyntax);
        }
    }
}

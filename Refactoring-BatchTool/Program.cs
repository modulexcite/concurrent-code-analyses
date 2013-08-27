using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Utilities;

namespace Refactoring_BatchTool
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\Weather\Weather.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\topaz-fuel-card-windows-phone\Topaz Fuel Card.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\Mono.Data.Sqlite\Mono.Data.Sqlite.sln";
        private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\WAZDash\WAZDash7.1.sln";

        private static int _numCandidates = 0;
        private static int _numRefactoringExceptions = 0;
        private static int _numNotImplementedExceptions = 0;

        static void Main()
        {
            Logger.Info("Hello, world!");

            try
            {
                DoWork(SolutionFile);
            }
            catch (NotImplementedException e)
            {
                Logger.Error("Not implemented: {0}: {1}", e.Message, e);
            }
            catch (Exception e)
            {
                Logger.Error("Caught exception: {0}: {1}", e.Message, e);
            }

            var numFailedRefactorings = _numRefactoringExceptions + _numNotImplementedExceptions;
            var numSuccesfulRefactorings = _numCandidates - numFailedRefactorings;

            Logger.Info("!!! REFACTORING RESULTS !!!");
            Logger.Info(" * Total number of candidates for refactoring: {0}", _numCandidates);
            Logger.Info(" * Number of succesful refactorings          : {0}", numSuccesfulRefactorings);
            Logger.Info(" * Number of failed refactorings             : {0}", numFailedRefactorings);
            Logger.Info("    - RefactoringExceptions   : {0}", _numRefactoringExceptions);
            Logger.Info("    - NotImplementedExceptions: {0}", _numNotImplementedExceptions);
            Logger.Info("!!! END OF RESULTS !!!");

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static void DoWork(String solutionPath)
        {
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
                .Where(document => document.FilePath.EndsWith(".cs"));

            solution = documents.Aggregate(solution,
                (sln, doc) => CheckDocument(doc, workspace, sln));

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

            var annotater = new APMBeginInvocationAnnotater();
            var annotatedDocument = annotater.Annotate(document);

            if (annotater.NumAnnotations == 0)
            {
                Logger.Trace("Document does not contain APM instances.");
                return solution;
            }

            Logger.Trace("Found {0} APM instances. Refactoring one-by-one ...", annotater.NumAnnotations);
            _numCandidates += annotater.NumAnnotations;

            var numErrors = solution.CompilationErrorCount();

            var refactoredSolution = solution;
            var refactoredDocument = annotatedDocument;
            for (var index = 0; index < annotater.NumAnnotations; index++)
            {
                var beginXxxSyntax = annotatedDocument.GetAnnotatedInvocation(index);

                // TODO: In the LocalDeclarationStatementSyntax case, the declared variable must be checked for non-use.
                if (beginXxxSyntax.ContainingStatement() is ExpressionStatementSyntax ||
                    beginXxxSyntax.ContainingStatement() is LocalDeclarationStatementSyntax)
                {
                    var oldSolution = refactoredSolution;
                    try
                    {
                        refactoredDocument = ExecuteRefactoring(workspace, refactoredDocument, refactoredSolution, index);

                        refactoredSolution = refactoredSolution
                            .WithDocumentSyntaxRoot(
                                refactoredDocument.Id,
                                refactoredDocument.GetSyntaxRootAsync().Result
                            );

                        if (refactoredSolution.CompilationErrorCount() > numErrors)
                        {
                            Logger.Error("Refactoring {0} caused new compilation errors. It will not be applied.", index);
                            refactoredSolution = oldSolution;
                        }
                    }
                    catch (RefactoringException e)
                    {
                        Logger.Error("Refactoring failed: index={0}: {1}: {2}", index, e.Message, e);
                        refactoredSolution = oldSolution;

                        _numRefactoringExceptions++;
                    }
                    catch (NotImplementedException e)
                    {
                        Logger.Error("Not implemented: index={0}: {0}: {1}", index, e.Message, e);
                        refactoredSolution = oldSolution;

                        _numNotImplementedExceptions++;
                    }
                }
                else
                {
                    Logger.Warn("APM Begin invocation containing statement is not yet supported: index={0}: {1}: statement: {2}",
                        index,
                        beginXxxSyntax.ContainingStatement().Kind,
                        beginXxxSyntax.ContainingStatement()
                    );
                }
            }

            return refactoredSolution;
        }

        private static Document ExecuteRefactoring(Workspace workspace, Document document, Solution solution, int index)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (document == null) throw new ArgumentNullException("document");

            var syntax = ((SyntaxTree)document.GetSyntaxTreeAsync().Result).GetRoot();

            Logger.Info("Refactoring annotated document: index={0}", index);
            Logger.Info("=== CODE TO REFACTOR ===\n{0}=== END OF CODE ===", syntax);

            var startTime = DateTime.UtcNow;

            var refactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(document, solution, workspace, index);

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Info("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Info("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredSyntax.Format(workspace));

            return document.WithSyntaxRoot(refactoredSyntax);
        }

        private static async Task<Solution> TryLoadSolutionAsync(this MSBuildWorkspace workspace, string solutionPath)
        {
            Logger.Trace("Trying to load solution file: {0}", solutionPath);

            try
            {
                return await workspace.OpenSolutionAsync(solutionPath);
            }
            catch (Exception ex)
            {
                Logger.Warn("Solution not analyzed: {0}: Reason: {1}", solutionPath, ex.Message);

                return null;
            }
        }
    }
}

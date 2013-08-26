using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
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
        private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\topaz-fuel-card-windows-phone\Topaz Fuel Card.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\Mono.Data.Sqlite\Mono.Data.Sqlite.sln";
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\WAZDash\WAZDash7.1.sln";

        static void Main()
        {
            Logger.Info("Hello, world!");

            try
            {
                DoWork();
            }
            catch (NotImplementedException e)
            {
                Logger.Error("Not implemented: {0}", e.Message, e);
            }
            catch (Exception e)
            {
                Logger.Error("Caught exception: {0}: {1}", e.Message, e);
            }

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static void DoWork()
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.TryLoadSolutionAsync(SolutionFile).Result;

            if (solution == null)
            {
                Logger.Error("Failed to load solution file: {0}", SolutionFile);
                return;
            }

            var documents = solution.Projects
                                    .SelectMany(project => project.Documents)
                                    .Where(document => document.FilePath.EndsWith(".cs"));

            foreach (var document in documents)
            {
                solution = CheckDocument(document, workspace, solution);
            }

            //if (!workspace.TryApplyChanges(solution))
            //{
            //    Logger.Error("Failed to apply changes in solution to workspace");
            //}
        }

        private static Solution CheckDocument(Document document, Workspace workspace, Solution solution)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (solution == null) throw new ArgumentNullException("solution");

            Logger.Debug("Checking document: {0}", document.FilePath);

            var annotater = new APMBeginInvocationAnnotater();
            var annotatedDocument = annotater.Annotate(document);

            if (annotater.NumAnnotations == 0)
            {
                Logger.Trace("Document does not contain APM instances.");
                return solution;
            }

            Logger.Trace("Found {0} APM instances. Refactoring one-by-one ...", annotater.NumAnnotations);

            var refactoredSolution = solution;
            var refactoredDocument = annotatedDocument;
            for (var index = 0; index < annotater.NumAnnotations; index++)
            {
                var beginXxxSyntax = annotatedDocument.GetAnnotatedInvocation(index);

                // TODO: In the LocalDeclarationStatementSyntax case, the declared variable must be checked for non-use.
                if (beginXxxSyntax.ContainingStatement() is ExpressionStatementSyntax ||
                    beginXxxSyntax.ContainingStatement() is LocalDeclarationStatementSyntax)
                {

                    try
                    {
                        refactoredDocument = ExecuteRefactoring(workspace, refactoredDocument, index);
                    }
                    catch (RefactoringException e)
                    {
                        Logger.Error("Refactoring failed: {0}", e.Message, e);

                        throw new Exception("Refactoring failed: " + e.Message, e);
                    }

                    refactoredSolution = refactoredSolution.WithDocumentSyntaxRoot(
                        refactoredDocument.Id,
                        refactoredDocument.GetSyntaxRootAsync().Result
                    );
                }
                else
                {
                    Logger.Warn("APM Begin invocation containing statement is not yet supported: {0}: statement: {1}",
                        beginXxxSyntax.ContainingStatement().Kind, beginXxxSyntax.ContainingStatement());
                }
            }

            return refactoredSolution;
        }

        private static Document ExecuteRefactoring(Workspace workspace, Document annotatedDocument, int index)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (annotatedDocument == null) throw new ArgumentNullException("annotatedDocument");

            var annotatedSyntax = ((SyntaxTree)annotatedDocument.GetSyntaxTreeAsync().Result).GetRoot();

            Logger.Info("Refactoring annotated document:");
            Logger.Info("=== CODE TO REFACTOR ===\n{0}=== END OF CODE ===", annotatedSyntax);

            var startTime = DateTime.UtcNow;

            var refactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(annotatedDocument, workspace, index);

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Info("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Info("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredSyntax.Format(workspace));

            return annotatedDocument.WithSyntaxRoot(refactoredSyntax);
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

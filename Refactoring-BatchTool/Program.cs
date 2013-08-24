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
                Logger.Error("Caught exception: {0}", e.Message, e);
            }

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static void DoWork()
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
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
                CheckDocument(document, workspace, solution);
            }
        }

        private static void CheckDocument(Document document, Workspace workspace, Solution solution)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (workspace == null) throw new ArgumentNullException("workspace");

            var tree = (SyntaxTree)document.GetSyntaxTreeAsync().Result;
            var compilation = CompilationUtils.CreateCompilation(tree);
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetRoot();

            var searcher = new BeginXxxSearcher(model);
            searcher.Visit(syntax);

            var beginXxxSyntax = searcher.BeginXxxSyntax;

            if (beginXxxSyntax == null) return;

            Logger.Info("Found APM Begin method: {0}", beginXxxSyntax);
            Logger.Info("  At: {0}:{1}", beginXxxSyntax.SyntaxTree.FilePath, beginXxxSyntax.Span.Start);

            // TODO: In the LocalDeclarationStatementSyntax case, the declared variable must be checked for non-use.
            if (beginXxxSyntax.ContainingStatement() is ExpressionStatementSyntax
                || beginXxxSyntax.ContainingStatement() is LocalDeclarationStatementSyntax)
            {
                var annotatedInvocation = beginXxxSyntax.WithAdditionalAnnotations(new RefactorableAPMInstance());
                var annotatedSyntax = syntax.ReplaceNode(beginXxxSyntax, annotatedInvocation);

                var annotatedDocument = document.WithSyntaxRoot(annotatedSyntax);

                Document refactoredDocument;
                try
                {
                    refactoredDocument = ExecuteRefactoring(workspace, annotatedDocument);
                }
                catch (RefactoringException e)
                {
                    Logger.Error("Refactoring failed: {0}", e.Message, e);

                    throw new Exception("Refactoring failed: " + e.Message, e);
                }

                Logger.Trace("Recursively checking for more APM Begin method invocations ...");
                var refactoredSolution = solution.WithDocumentSyntaxRoot(document.Id, refactoredDocument.GetSyntaxRootAsync().Result);
                if (!workspace.TryApplyChanges(refactoredSolution))
                {
                    throw new Exception("Workspace was changed during refactoring");
                }

                CheckDocument(refactoredSolution.GetDocument(document.Id), workspace, refactoredSolution);
            }
            else
            {
                Logger.Warn("APM Begin invocation containing statement is not yet supported: {0}: statement: {1}",
                    beginXxxSyntax.ContainingStatement().Kind, beginXxxSyntax.ContainingStatement());
            }
        }

        private static Document ExecuteRefactoring(Workspace workspace, Document annotatedDocument)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (annotatedDocument == null) throw new ArgumentNullException("annotatedDocument");

            var annotatedSyntax = ((SyntaxTree)annotatedDocument.GetSyntaxTreeAsync().Result).GetRoot();

            Logger.Info("Refactoring annotated document:");
            Logger.Info("=== CODE TO REFACTOR ===\n{0}=== END OF CODE ===", annotatedSyntax);

            var startTime = DateTime.UtcNow;

            var refactoredSyntax = RefactoringExtensions.RefactorAPMToAsyncAwait(annotatedDocument, workspace);

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Info("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Info("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredSyntax.Format(workspace));

            return annotatedDocument.WithSyntaxRoot(refactoredSyntax);
        }

        private static SyntaxTree CreateSyntaxTree(this SyntaxNode syntax)
        {
            if (syntax == null) throw new ArgumentNullException("syntax");

            return SyntaxTree.Create(syntax);
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

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

        private const string SolutionFile = @"C:\Users\david\Projects\UIUC\APM-to-AA-Test\APM-to-AA-Test.sln";

        static void Main()
        {
            Logger.Info("Hello, world!");

            DoWork();

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

            var trees = solution.Projects
                .SelectMany(project => project.Documents)
                .Where(document => document.FilePath.EndsWith(".cs"))
                .Select(document => document.GetSyntaxTreeAsync().Result)
                .OfType<SyntaxTree>();

            foreach (var tree in trees)
            {
                workspace.CheckTree(tree);
            }
        }

        private static void CheckTree(this MSBuildWorkspace workspace, SyntaxTree tree)
        {
            Logger.Trace("Checking tree:\n{0}", tree.GetRoot().Format(workspace));

            var compilation = CompilationUtils.CreateCompilation(tree);
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetRoot();

            var searcher = new BeginXxxSearcher(model);
            searcher.Visit(syntax);

            var beginXxxSyntax = searcher.BeginXxxSyntax;

            if (beginXxxSyntax != null)
            {
                Logger.Info("Found APM Begin method: {0}", beginXxxSyntax);
                Logger.Info("  At: {0}:{1}", beginXxxSyntax.SyntaxTree.FilePath, beginXxxSyntax.Span.Start);

                if (beginXxxSyntax.Parent is ExpressionStatementSyntax)
                {
                    var annotatedSyntax = AnnotatedSyntax(beginXxxSyntax, syntax);
                    var refactoredTree = workspace.ExecuteRefactoring(annotatedSyntax);

                    Logger.Trace("Recursively checking for more APM Begin method invocations ...");
                    workspace.CheckTree(refactoredTree);
                }
                else
                {
                    Logger.Warn("APM Begin invocation as non-ExpressionStatementSyntax is not yet supported");
                }
            }
        }

        private static SyntaxNode AnnotatedSyntax(InvocationExpressionSyntax beginXxxSyntax, SyntaxNode syntax)
        {
            var annotatedInvocation = (beginXxxSyntax.Parent as ExpressionStatementSyntax)
                                                     .WithAdditionalAnnotations(new RefactorableAPMInstance());

            var annotatedSyntax = syntax.ReplaceNode(
                beginXxxSyntax.Parent,
                annotatedInvocation
            );

            return annotatedSyntax;
        }

        private static SyntaxTree ExecuteRefactoring(this Workspace workspace, SyntaxNode annotatedSyntax)
        {
            Logger.Info("Refactoring annotated syntax tree ...");

            var startTime = DateTime.UtcNow;

            var refactoredTree = annotatedSyntax.CreateSyntaxTree()
                .RefactorAPMToAsyncAwait(workspace)
                .CreateSyntaxTree();

            var endTime = DateTime.UtcNow;
            var refactoringTime = endTime.Subtract(startTime).Milliseconds;

            Logger.Info("Refactoring completed in {0} ms.", refactoringTime);
            Logger.Info("=== REFACTORED CODE ===\n{0}=== END OF CODE ===", refactoredTree.GetRoot().Format(workspace));

            return refactoredTree;
        }

        private static SyntaxTree CreateSyntaxTree(this SyntaxNode annotatedSyntax)
        {
            return SyntaxTree.Create(annotatedSyntax);
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

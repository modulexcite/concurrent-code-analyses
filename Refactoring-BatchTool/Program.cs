using System;
using System.Linq;
using NLog;
using Refactoring;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace Refactoring_BatchTool
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string SolutionFile = @"C:\Users\david\Projects\UIUC\APM-to-AA-Test\APM-to-AA-Test.sln";

        static void Main()
        {
            Logger.Info("Hello, world!");

            var solution = TryLoadSolutionAsync(SolutionFile).Result;

            if (solution == null)
            {
                Logger.Error("Failed to load solution file: {0}", SolutionFile);
                return;
            }

            foreach (var project in solution.Projects)
            {
                Logger.Info("Project: {0}", project.FilePath);
            }

            var trees = solution.Projects
                                .SelectMany(project => project.Documents)
                                .Where(document => document.FilePath.EndsWith(".cs"))
                                .Select(document => document.GetSyntaxTreeAsync().Result)
                                .OfType<SyntaxTree>();

            foreach (var tree in trees)
            {
                var compilation = CompilationUtils.CreateCompilation(tree);
                var model = compilation.GetSemanticModel(tree);

                var searcher = new BeginXxxSearcher(model);
                searcher.Visit(tree.GetRoot());

                var beginXxxSyntax = searcher.BeginXxxSyntax;

                if (beginXxxSyntax != null)
                {
                    // TODO
                }
            }

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static async Task<Solution> TryLoadSolutionAsync(string solutionPath)
        {
            try
            {
                MSBuildWorkspace workspace = MSBuildWorkspace.Create();
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

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using Refactoring;
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

        private const string CandidatesDir = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\";

        static void Main()
        {
            Logger.Info("Hello, world!");

            TryRunOverSolutionFile(SolutionFile);

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static void TryRunOverSolutionFile(string solutionFile)
        {
            SolutionRefactoring refactoring;
            try
            {
                refactoring = RunOverSolutionFile(solutionFile);
            }
            catch (Exception e)
            {
                Logger.Error("%%% CRITICAL ERROR %%%");
                Logger.Error("%%% Caught unexpected exception during work on solution file: {0}", solutionFile);
                Logger.Error("%%% Caught exception: {0}:\n{1}", e.Message, e);

                return;
            }

            Logger.Info("!!! REFACTORING RESULTS !!!");
            Logger.Info("!!! * Total number of candidates for refactoring: {0}", refactoring.NumCandidates);
            Logger.Info("!!! * Number of succesful refactorings          : {0}", refactoring.NumSuccesfulRefactorings);
            Logger.Info("!!! * Number of failed refactorings             : {0}", refactoring.NumFailedRefactorings);
            Logger.Info("!!!    - RefactoringExceptions   : {0}", refactoring.NumRefactoringExceptions);
            Logger.Info("!!!    - NotImplementedExceptions: {0}", refactoring.NumNotImplementedExceptions);
            Logger.Info("!!!    - Other exceptions        : {0}", refactoring.NumOtherExceptions);
            Logger.Info("!!! END OF RESULTS !!!");
        }

        private static SolutionRefactoring RunOverSolutionFile(String solutionPath)
        {
            if (solutionPath == null) throw new ArgumentNullException("solutionPath");

            Logger.Trace("Loading solution file: {0}", solutionPath);

            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.TryLoadSolutionAsync(solutionPath).Result;

            if (solution == null)
            {
                Logger.Error("Failed to load solution file: {0}", solutionPath);

                throw new Exception("Failed to load solution file: " + solutionPath);
            }

            var refactoring = new SolutionRefactoring(workspace);
            refactoring.Run();

            return refactoring;
        }
    }
}

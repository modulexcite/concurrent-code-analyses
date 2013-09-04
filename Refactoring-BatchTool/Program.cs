using System;
using System.Collections.Generic;
using System.IO;
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
        //private const string SolutionFile = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\8digits-WindowsPhone-SDK-Sample-App\EightDigitsTest.sln";

        private const string CandidatesDir = @"C:\Users\david\Projects\UIUC\Candidates\Automatic\";
        private const int BatchSize = 100;

        private const string RefactoredAppsFile = @"C:\Users\david\Projects\UIUC\Logs\RefactoredApps.log";
        private static readonly string[] RefactoredApps =
            File.Exists(RefactoredAppsFile)
                ? File.ReadAllLines(RefactoredAppsFile)
                : new string[] { };

        static void Main()
        {
            Logger.Info("Hello, world!");

            var solutionFilePaths = Directory.GetDirectories(CandidatesDir)
                .SelectMany(app => Directory.GetFiles(app, "*.sln", SearchOption.AllDirectories))
                .Where(IsNotYetRefactored)
                .Take(BatchSize);

            foreach (var solutionFilePath in solutionFilePaths)
            {
                TryRunOverSolutionFile(solutionFilePath);
            }

            Console.WriteLine(@"Press any key to quit ...");
            Console.ReadKey();
        }

        private static bool IsNotYetRefactored(string subdir)
        {
            return !RefactoredApps.Any(s => subdir.Split('\\').Last().Equals(s));
        }

        private static void TryRunOverSolutionFile(string solutionFile)
        {
            Logger.Info("Running over solution file: {0}", solutionFile);

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

            File.AppendAllText(RefactoredAppsFile, solutionFile);
        }

        private static SolutionRefactoring RunOverSolutionFile(String solutionPath)
        {
            if (solutionPath == null) throw new ArgumentNullException("solutionPath");

            Logger.Trace("Loading solution file: {0}", solutionPath);

            SolutionRefactoring refactoring;
            using (var workspace = MSBuildWorkspace.Create())
            {
                var solution = workspace.TryLoadSolutionAsync(solutionPath).Result;

                if (solution == null)
                {
                    Logger.Error("Failed to load solution file: {0}", solutionPath);

                    throw new Exception("Failed to load solution file: " + solutionPath);
                }

                refactoring = new SolutionRefactoring(workspace);
                refactoring.Run();

                workspace.CloseSolution();
            }

            return refactoring;
        }
    }
}

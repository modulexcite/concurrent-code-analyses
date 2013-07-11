using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Exceptions;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.CSharp;

namespace TestApps
{
    class Test2
    {
        public static void execute()
        {
            //const string candidatesDir = @"C:\Users\david\Downloads\C# Projects\CodeplexMostDownloaded1000Projects";
            const string candidatesDir = @"C:\Users\david\Downloads\C# Projects\Candidates";

            Console.WriteLine("Searching {0} and all subdirectories for solution files ...", candidatesDir);
            var solutionFileNames = Directory.GetFiles(candidatesDir, "*.sln", SearchOption.AllDirectories);

            Console.WriteLine("Searching for interesting projects ...");
            foreach (var solutionFileName in solutionFileNames)
            {
                PrintInterestingProjects(solutionFileName);
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }

        private static void PrintInterestingProjects(string solutionFileName)
        {
            var solution = TryLoadSolution(solutionFileName);

            if (solution != null)
            {
                var projects = solution.Projects
                    .Where(project => project.IsInteresting());

                if (projects.Any())
                {
                    Console.WriteLine("- {0}", solutionFileName);
                    foreach (var project in projects)
                    {
                        Console.WriteLine("-- {0}", project.FilePath);
                    }
                }
            }
        }

        private static ISolution TryLoadSolution(string filename)
        {
            try
            {
                return Solution.Load(filename);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }

    static class Extensions
    {
        public static bool IsInteresting(this IProject project)
        {
            try
            {
                //return project.IsCSProject() && project.IsWP8Project();
                return true;
            }
            catch (InvalidProjectFileException)
            {
                return false;
            }
        }
    }
}

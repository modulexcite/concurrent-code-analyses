using NLog;
using Roslyn.Services;
using System;
using System.IO;
using System.Linq;

namespace Classifier
{
    internal class Program
    {
        public static readonly Logger Results = LogManager.GetLogger("Results");

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            const string candidatesDir = @"C:\Users\david\Projects\UIUC\Candidates";

            var solutions = Directory.GetDirectories(candidatesDir)
                     .SelectMany(directory => Directory.GetFiles(directory, "*.sln", SearchOption.AllDirectories))
                     .Select(TryLoadSolution)
                     .Where(sln => sln != null);

            var walker = new SolutionWalker();
            foreach (var solution in solutions)
            {
                walker.VisitSolution(solution);
            }

            Console.WriteLine(@"Program finished. Press any key to quit ...");
            Console.ReadKey();
        }

        private static ISolution TryLoadSolution(string filename)
        {
            Log.Trace("Trying to load solution from file: {0}", filename);

            try
            {
                return Solution.Load(filename);
            }
            catch (Exception e)
            {
                Log.Warn(@"Failed to load solution: {0}: {1}", filename, e.Message, e);

                return null;
            }
        }
    }
}

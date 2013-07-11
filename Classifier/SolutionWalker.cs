using System;
using NLog;
using Roslyn.Services;

namespace Classifier
{
    internal class SolutionWalker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly CalledMethodClassifyingWalker _walker = new CalledMethodClassifyingWalker();

        public void VisitSolution(ISolution solution)
        {
            Log.Trace("Visiting solution: {0}", solution.FilePath);

            foreach (var project in solution.Projects)
            {
                TryVisitProject(project);
            }
        }

        private void TryVisitProject(IProject project)
        {
            try
            {
                VisitProject(project);
            }
            catch (Exception e)
            {
                Log.Error("Failed to inspect project: {0}: {1}", project.FilePath, e.Message, e);
            }
        }

        private void VisitProject(IProject project)
        {
            Log.Trace("Visiting project: {0}", project.FilePath);

            foreach (var document in project.Documents)
            {
                TryVisitDocument(document);
            }
        }

        private void TryVisitDocument(IDocument document)
        {
            try
            {
                _walker.Visit(document);
            }
            catch (Exception e)
            {
                Log.Error("Failed to visit document: {0}: {1}", document.FilePath, e.Message, e);
            }
        }
    }
}

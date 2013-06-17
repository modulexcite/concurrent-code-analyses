using System;
using System.Collections.Generic;
using Roslyn.Services;
using System.IO;
using Microsoft.Build.Exceptions;

namespace Analysis
{
    public abstract class ProjectAnalysisBase
    {
        private readonly string _dirName;

        protected ISolution CurrentSolution;

        private readonly ProjectAnalysisSummary _summary;

        protected ProjectAnalysisBase(string dirName, ProjectAnalysisSummary summary)
        {
            _dirName = dirName;
            _summary = summary;
        }

        public void Analyze()
        {
            var solutionPaths = Directory.GetFiles(_dirName, "*.sln", SearchOption.AllDirectories);
            foreach (var solutionPath in solutionPaths)
            {
                CurrentSolution = Solution.Load(solutionPath);
                foreach (var project in CurrentSolution.Projects)
                {
                    AnalyzeProject(project);
                }
            }

            OnAnalysisCompleted();
        }

        public void AnalyzeProject(IProject project)
        {
            IEnumerable<IDocument> documents;
            try
            {
                _summary.AddProject(project);

                if (!project.IsCSProject())
                    return;

                documents = project.Documents;
                if (documents == null)
                    return;
            }
            catch (Exception ex)
            {
                if (ex is InvalidProjectFileException ||
                    ex is FormatException ||
                    ex is ArgumentException ||
                    ex is PathTooLongException)
                {
                    _summary.AddUnanalyzedProject();
                }
                else
                {
                    throw;
                }
                return;
            }

            foreach (var document in documents)
            {
                AnalyzeDocument(document);
            }
        }

        protected abstract void AnalyzeDocument(IDocument document);

        protected abstract void OnAnalysisCompleted();
    }
}

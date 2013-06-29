using System;
using System.Collections.Generic;
using NLog;
using System.IO;
using System.Linq;
using Roslyn.Services;

using Microsoft.Build.Exceptions;

namespace Analysis
{
    public abstract class AnalysisBase
    {
        protected static readonly Logger Log = LogManager.GetLogger("Console");
 

        private readonly string _dirName;
        private readonly string _appName;

        protected ISolution CurrentSolution;

        public AnalysisResultBase Result
        {
            get { return ResultObject; }
        }
        public abstract AnalysisResultBase ResultObject { get; }

        protected AnalysisBase(string dirName, string appName)
        {
            _dirName = dirName;
            _appName = appName;
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
                if (!project.IsCSProject())
                    return;

                Result.AddProject(project);

                documents = project.Documents;

                if (documents == null)
                {
                    Result.AddUnanalyzedProject();
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ex is InvalidProjectFileException ||
                    ex is FormatException ||
                    ex is ArgumentException ||
                    ex is PathTooLongException)
                {
                    Log.Info("Project not analyzed: {0}", project.FilePath, ex);
                    Result.AddUnanalyzedProject();
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

        protected void OnAnalysisCompleted()
        {
            Result.WriteSummaryLog();
        }

    }
}

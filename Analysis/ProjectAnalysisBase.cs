using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Services;
using System.IO;
using Microsoft.Build.Exceptions;
using Utilities;

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
            _summary.NumTotalProjects++;

            if (!project.IsCSProject())
                return;

            IEnumerable<IDocument> documents;
            try
            {
                AddProject(project);
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
                    _summary.NumUnanalyzedProjects++;
                }
                else
                {
                    throw;
                }
                return;
            }

            foreach (var document in documents)
                AnalyzeDocument(document);
        }

        public void DetectProject(IProject project)
        {
            if (project.IsWPProject())
            {
                _summary.NumPhoneProjects++;
            }

            if (project.IsWP8Project())
            {
                _summary.NumPhone8Projects++;
            }

            if (project.IsAzureProject())
            {
                _summary.NumAzureProjects++;
            }

            if (project.IsNet40Project())
            {
                _summary.NumNet4Projects++;
            }
            else if (project.IsNet45Project())
            {
                _summary.NumNet45Projects++;
            }
            else
            {
                _summary.NumOtherNetProjects++;
            }
        }

        public abstract void AnalyzeDocument(IDocument document);

        public abstract void OnAnalysisCompleted();

        public void WriteResults(StreamWriter logFileWriter)
        {
            var logText = _summary.AppName + "," + _summary.NumTotalProjects + "," + _summary.NumUnloadedProjects + "," + _summary.NumUnanalyzedProjects + "\r\n";

            logFileWriter.Write(logText);
        }

    }
}

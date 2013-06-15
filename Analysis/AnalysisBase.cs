using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Services;
using System.IO;
using Microsoft.Build.Exceptions;
using Utilities;

namespace Analysis
{
    public abstract class AnalysisBase
    {
        private readonly string _dirName;
        private readonly List<IProject> _projects;
        private readonly Dictionary<ISolution, List<IProject>> _projectsBySolutions;
        private bool _isUsingSolutionFiles;

        protected string AppName;

        protected ISolution CurrentSolution;

        protected int NumUnloadedProjects;
        protected int NumTotalProjects;
        protected int NumUnanalyzedProjects;

        private AnalysisSummary _summary;

        protected AnalysisBase(string appName, string dirName, AnalysisSummary summary)
        {
            AppName = appName;
            _dirName = dirName;
            _projects = new List<IProject>();
            _projectsBySolutions = new Dictionary<ISolution, List<IProject>>();

            _summary = summary;
        }

        public void LoadSolutions()
        {
            _isUsingSolutionFiles = true;

            var solutionPaths = Directory.GetFiles(_dirName, "*.sln", SearchOption.AllDirectories);
            foreach (var solutionPath in solutionPaths)
            {
                var solution = Solution.Load(solutionPath);
                _projectsBySolutions.Add(solution, solution.Projects.ToList());
                NumTotalProjects += solution.Projects.Count();
            }
        }

        public void Analyze()
        {
            if (_isUsingSolutionFiles)
            {
                foreach (var solution in _projectsBySolutions.Keys)
                {
                    foreach (var project in _projectsBySolutions[solution])
                        AnalyzeProject(project);
                }
            }
            else
            {
                foreach (var project in _projects)
                    AnalyzeProject(project);
            }
            OnAnalysisCompleted();
        }

        public void AnalyzeProject(IProject project)
        {
            if (!project.IsCSProject())
                return;

            IEnumerable<IDocument> documents = null;
            try
            {
                DetectTarget(project);
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
                    NumUnanalyzedProjects++;
                }
                else
                {
                    throw;
                }
                return;
            }

            foreach (var document in documents)
                AnalyzeDocument(document);
            //ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
        }

        private void DetectTarget(IProject project)
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

        public void WriteResults(String logFile)
        {
            var logText = AppName + "," + NumTotalProjects + "," + NumUnloadedProjects + "," + NumUnanalyzedProjects + "\r\n";

            Helper.WriteLogger(logFile, logText);
        }

    }
}

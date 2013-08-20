using Microsoft.Build.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Utilities;

namespace Analysis
{
    public abstract class AnalysisBase
    {
        private readonly string _dirName;
        private readonly string _appName;

        protected MSBuildWorkspace workspace;
        protected Solution CurrentSolution;
        protected bool hasPhoneProjectInThisSolution;

        public AnalysisResultBase Result
        {
            get { return ResultObject; }
        }

        public abstract AnalysisResultBase ResultObject { get; }

        protected AnalysisBase(string dirName, string appName)
        {
            _dirName = dirName;
            _appName = appName;
            workspace = MSBuildWorkspace.Create();
            
        }

        public void Analyze()
        {
            var solutionPaths = from f in Directory.GetFiles(_dirName, "*.sln", SearchOption.AllDirectories)
                                let directoryName = Path.GetDirectoryName(f)
                                where !directoryName.Contains(@"\tags") &&
                                      !directoryName.Contains(@"\branches")
                                select f;

            foreach (var solutionPath in solutionPaths)
            {

                CurrentSolution = TryLoadSolution(solutionPath);

                if ((CurrentSolution = TryLoadSolution(solutionPath)) != null)
                    foreach (var project in CurrentSolution.Projects)
                        AnalyzeProject(project);
                workspace.CloseSolution();
            }

            if (Result.generalResults.NumTotalProjects == 0)
            {
                var projectPaths = from f in Directory.GetFiles(_dirName, "*.csproj", SearchOption.AllDirectories)
                                    let directoryName = Path.GetDirectoryName(f)
                                    where !directoryName.Contains(@"\tags") &&
                                          !directoryName.Contains(@"\branches")
                                    select f;
                foreach (var projectPath in projectPaths)
                {
                    try
                    {
                        var project = workspace.OpenProjectAsync(projectPath).Result;
                        AnalyzeProject(project);
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidProjectFileException ||
                            ex is FormatException ||
                            ex is ArgumentException ||
                            ex is PathTooLongException)
                        {
                            Logs.Log.Info("Project not analyzed: {0}: Reason: {1}", projectPath, ex.Message);
                        }
                        else
                            throw;
                    }
                }
            }

            OnAnalysisCompleted();
        }

        private Solution TryLoadSolution(string solutionPath)
        {
            try
            {
                return workspace.OpenSolutionAsync(solutionPath).Result;
            }
            catch (Exception ex)
            {
                Logs.Log.Info("Solution not analyzed: {0}: Reason: {1}", solutionPath, ex.Message);
                return null;
            }
        }

        public void AnalyzeProject(Project project)
        {
            Result.AddProject();
            IEnumerable<Document> documents;

            if ((documents = TryLoadProject(project)) != null
                && project.IsCSProject())
            {
                Enums.ProjectType type = project.GetProjectType();

                Result.AddAnalyzedProject(type);
                // Filtering projects according to their type, depending on the type of the analysis
                if (FilterProject(type))
                {
                    Result.CurrentAnalyzedProjectType = type;
                    foreach (var document in documents)
                        AnalyzeDocument(document);
                }
            }
            else
            {
                Result.AddUnanalyzedProject();
            }
        }

        protected abstract bool FilterProject(Enums.ProjectType type);

        // I did not make it extension method, because it is better to see all exception handling in this file.
        private static IEnumerable<Document> TryLoadProject(Project project)
        {
            IEnumerable<Document> documents = null;
            try
            {
                documents = project.Documents;
                var totalDocuments = documents.Count();
            }
            catch (Exception ex)
            {
                if (ex is FormatException ||
                    ex is ArgumentException ||
                    ex is PathTooLongException)
                {
                    Logs.Log.Info("Project not analyzed: {0}: Reason: {1}", project.FilePath, ex.Message);
                }
                else
                    throw;
            }
            return documents;
        }

    
        protected void AnalyzeDocument(Document document)
        {
            if (FilterDocument(document))
            {
                var root = (SyntaxNode)document.GetSyntaxRootAsync().Result;
                var sloc = root.CountSLOC();
                Result.generalResults.NumTotalSLOC += sloc;

                if (Result.CurrentAnalyzedProjectType == Enums.ProjectType.WP7)
                    Result.generalResults.SLOCWP7 += sloc;
                else
                    Result.generalResults.SLOCWP8 += sloc;
                try
                {
                    VisitDocument(document, root);
                }
                catch (InvalidProjectFileException ex)
                {
                    Logs.Log.Info("Document not analyzed: {0}: Reason: {1}", document.FilePath, ex.Message);
                    Result.generalResults.NumTotalSLOC -= sloc;
                    if (Result.CurrentAnalyzedProjectType == Enums.ProjectType.WP7)
                        Result.generalResults.SLOCWP7 -= sloc;
                    else
                        Result.generalResults.SLOCWP8 -= sloc;
                }

            }
        }

        protected abstract bool FilterDocument(Document document);

        protected abstract void VisitDocument(Document document, SyntaxNode root);

        protected void OnAnalysisCompleted()
        {
            Result.WriteSummaryLog();
        }
    }
}
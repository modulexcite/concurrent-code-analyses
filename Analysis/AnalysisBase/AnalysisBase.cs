using Microsoft.Build.Exceptions;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
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

        protected ISolution CurrentSolution;
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
                //TryUpgradeToVS2012(solutionPath);  // enable when you are first exploring the code repository

                CurrentSolution = TryLoadSolution(solutionPath);

                if ((CurrentSolution = TryLoadSolution(solutionPath)) != null)
                    foreach (var project in CurrentSolution.Projects)
                        AnalyzeProject(project);
            }

            OnAnalysisCompleted();
        }

        private static ISolution TryLoadSolution(string solutionPath)
        {
            try
            {
                return Solution.Load(solutionPath);
            }
            catch (Exception ex)
            {
                Logs.Log.Info("Solution not analyzed: {0}: Reason: {1}", solutionPath, ex.Message);
                return null;
            }
        }

        public void AnalyzeProject(IProject project)
        {
            Result.AddProject();
            IEnumerable<IDocument> documents;

            if ((documents = TryLoadProject(project)) != null
                && project.IsCSProject())
            {
                Enums.ProjectType type = project.GetProjectType();

                // Filtering projects according to their type, depending on the type of the analysis
                if (FilterProject(type))
                {
                    Result.AddAnalyzedProject(type);
                    foreach (var document in documents)
                        AnalyzeDocument(document);
                }
                else
                {
                    Result.AddUnanalyzedProject();
                }
            }
            else
            {
                Result.AddUnanalyzedProject();
            }
        }

        protected abstract bool FilterProject(Enums.ProjectType type);

        // I did not make it extension method, because it is better to see all exception handling in this file.
        private static IEnumerable<IDocument> TryLoadProject(IProject project)
        {
            IEnumerable<IDocument> documents = null;
            try
            {
                documents = project.Documents;
                var totalDocuments = documents.Count();
            }
            catch (Exception ex)
            {
                if (ex is InvalidProjectFileException ||
                    ex is FormatException ||
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

        public static void UpgradeToVS2012(string path)
        {
            var command = @"devenv /upgrade " + "\"" + path + "\"";
            var info = new ProcessStartInfo("cmd.exe", "/C " + command)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    //UseShellExecute = false
                };

            var p = Process.Start(info);
            p.WaitForExit();
            p.Close();

            string dir = Path.GetDirectoryName(path) + @"\Backup\";
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        /// <summary>
        /// Try to upgrade the solution to VS 2012.
        /// </summary>
        /// <param name="solutionPath">Filename of the solution to try to upgrade.</param>
        /// <returns>true if the upgrade was succesful, otherwise false.</returns>
        private static bool TryUpgradeToVS2012(string solutionPath)
        {
            try
            {
                UpgradeToVS2012(solutionPath);
            }
            catch (Exception ex)
            {
                Logs.Log.Info("Solution could not be upgraded: {0}: Reason: {1}", solutionPath, ex.Message);
                return false;
            }
            return true;
        }

        protected void AnalyzeDocument(IDocument document)
        {
            if (FilterDocument(document))
            {
                var root = (SyntaxNode)document.GetSyntaxTree().GetRoot();
                var sloc = root.CountSLOC();
                Result.generalResults.NumTotalSLOC += sloc;
                try
                {
                    VisitDocument(document, root);
                }
                catch (InvalidProjectFileException ex)
                {
                    Logs.Log.Info("Document not analyzed: {0}: Reason: {1}", document.FilePath, ex.Message);
                    Result.generalResults.NumTotalSLOC -= sloc;
                }
            }
        }

        protected abstract bool FilterDocument(IDocument document);

        protected abstract void VisitDocument(IDocument document, SyntaxNode root);

        protected void OnAnalysisCompleted()
        {
            Result.WriteSummaryLog();
        }
    }
}
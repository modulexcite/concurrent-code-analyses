using System;
using System.Collections.Generic;
using NLog;
using System.IO;
using System.Linq;
using Roslyn.Services;
using Microsoft.Build.Exceptions;
using System.Diagnostics;

namespace Analysis
{
    public abstract class AnalysisBase
    {
        protected static readonly Logger Log = LogManager.GetLogger("Console");


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
            var solutionPaths = Directory.GetFiles(_dirName, "*.sln", SearchOption.AllDirectories);
            foreach (var solutionPath in solutionPaths)
            {
                if (TryUpgradeToVS2012(solutionPath))
                {
                    continue;
                }

                CurrentSolution = TryLoadSolution(solutionPath);

                if (CurrentSolution == null)
                {
                    continue;
                }

                foreach (var project in CurrentSolution.Projects)
                {
                    AnalyzeProject(project);
                }
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
                Log.Info("Solution not analyzed: {0}: Reason: {1}", solutionPath, ex.Message);
                return null;
            }
        }

        public void AnalyzeProject(IProject project)
        {
            IEnumerable<IDocument> documents;
            try
            {
                if (!project.IsCSProject())
                    return;

                AnalysisResultBase.ProjectType type = Result.AddProject(project);

                documents = project.Documents;

                // If the project is not WP, ignore it! 
                if (type != AnalysisResultBase.ProjectType.WP7 && type != AnalysisResultBase.ProjectType.WP8)
                {
                    Result.AddUnanalyzedProject();
                    return;
                }
                //Result.WritePhoneProjects(project);


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
                try
                {
                    AnalyzeDocument(document);
                }
                catch (InvalidProjectFileException ex)
                {
                    Log.Info("Document not analyzed: {0}", document.FilePath, ex);
                }
            }
        }


        public static void UpgradeToVS2012(string path)
        {
            var command = @"devenv /upgrade " + "\"" + path + "\"";
            var info = new ProcessStartInfo("cmd.exe", "/C " + command)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
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
            catch (Exception e)
            {
                Log.Info("Solution could not be upgraded: {0}: Reason: {1}", solutionPath, e.Message);
                return true;
            }
            return false;
        }

        protected abstract void AnalyzeDocument(IDocument document);

        protected void OnAnalysisCompleted()
        {
            Result.WriteSummaryLog();
        }

    }
}

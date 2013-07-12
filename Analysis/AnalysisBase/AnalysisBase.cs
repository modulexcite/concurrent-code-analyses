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
                try
                {
                    UpgradeToVS2012(solutionPath);
                    CurrentSolution = Solution.Load(solutionPath);
                }
                catch (Exception ex)
                {
                    Log.Info("Solution not analyzed: {0}: Reason: {1}", solutionPath, ex.Message);
                    continue;
                }

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

                AnalysisResultBase.ProjectType type= Result.AddProject(project);

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
               AnalyzeDocument(document);
            }
        }


        public void UpgradeToVS2012(string path)
        {
            var command = @"devenv " + path + @" /upgrade";
            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", "/C " + command);
            info.WindowStyle = ProcessWindowStyle.Hidden;

            Process p = Process.Start(info);
            p.WaitForExit();
            string dir = Path.GetDirectoryName(path) + @"\Backup\";
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        protected abstract void AnalyzeDocument(IDocument document);

        protected void OnAnalysisCompleted()
        {
            Result.WriteSummaryLog();
        }

    }
}

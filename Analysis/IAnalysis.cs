using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using System.IO;
using Microsoft;
using Microsoft.Build;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Evaluation;

namespace Analysis
{
    public abstract class IAnalysis
    {
        public string appName;
        string dirName;
        public int numUnloadedProjects;
        public int numTotalProjects;
        public int numUnanalyzedProjects;

        List<IProject> projects;

        public IAnalysis(string appName, string dirName)
        {
            this.appName = appName;
            this.dirName = dirName;
            projects = new List<IProject>();
        }


        public void load()
        {

            // if (b.EndsWith("tags") || b.EndsWith("branch"))
            var projectPaths = Directory.GetFiles(dirName, "*.csproj", SearchOption.AllDirectories);
            numTotalProjects = projectPaths.Count();

            foreach (string projectPath in projectPaths)
            {

                try
                {
                    var project = Roslyn.Services.Solution.LoadStandAloneProject(projectPath);
                    projects.Add(project);
                }
                catch (Exception ex)
                {
                    if (ex is InvalidProjectFileException ||
                        ex is FormatException ||
                        ex is ArgumentException ||
                        ex is PathTooLongException)
                    {
                        numUnloadedProjects++;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

        }


        public void analyze()
        {

            foreach (IProject project in projects)
                analyzeProject(project);
            onAnalysisCompleted();
        }

        public void analyzeProject(IProject project)
        {

            // Compilation compilation = (Compilation)project.GetCompilation();


            try
            {
                foreach (IDocument document in project.Documents)
                    analyzeDocument(document);
            }
            catch (Exception ex)
            {
                if (ex is InvalidProjectFileException ||
                    ex is FormatException ||
                    ex is ArgumentException ||
                    ex is PathTooLongException)
                {
                    numUnanalyzedProjects++;
                }
                else
                {
                    throw;
                }
            }
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();

        }

        public abstract void analyzeDocument(IDocument document);


        public abstract void onAnalysisCompleted();

    }
}

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
using Utilities;

namespace Analysis
{
    public abstract class AnalysisBase
    {
        public string appName;
        string dirName;
        public int numUnloadedProjects;
        public int numTotalProjects;
        public int numUnanalyzedProjects;

        List<IProject> projects;
        Dictionary<ISolution, List<IProject>> projectsBySolutions;
        bool isUsingSolutionFiles;
        public ISolution currentSolution;
        public CommonCompilation currentCompilation;
        public int numPhoneProjects;
        public int numPhone8Projects;
        public int numAzureProjects;
        public int numNet4Projects;
        public int numNet45Projects;
        public int numOtherNetProjects;



        public AnalysisBase(string appName, string dirName)
        {
            this.appName = appName;
            this.dirName = dirName;
            projects = new List<IProject>();
            projectsBySolutions = new Dictionary<ISolution, List<IProject>>();
        }

        public void LoadSolutions()
        {
            isUsingSolutionFiles = true;
            var solutionPaths = Directory.GetFiles(dirName, "*.sln", SearchOption.AllDirectories);
            foreach (string solutionPath in solutionPaths)
            {
                var solution = Roslyn.Services.Solution.Load(solutionPath);
                projectsBySolutions.Add(solution, solution.Projects.ToList());
                numTotalProjects += solution.Projects.Count();
            }

        }
        public void LoadProjects()
        {
            isUsingSolutionFiles = false;
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


        public void Analyze()
        {
            if (isUsingSolutionFiles)
            {
                foreach (ISolution solution in projectsBySolutions.Keys)
                {
                    currentSolution = solution;
                    foreach (IProject project in projectsBySolutions[solution])
                        AnalyzeProject(project);
                }
            }
            else
            {
                foreach (IProject project in projects)
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
                if (!DetectTarget(project))
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
                    numUnanalyzedProjects++;
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

        private Boolean DetectTarget(IProject project)
        {
            if (project.IsWPProject())
            {
                numPhoneProjects++;
            }

            if (project.IsWP8Project())
            {
                numPhone8Projects++;
            }

            if (project.IsAzureProject())
            {
                numAzureProjects++;
            }

            if (project.IsNet40Project())
            {
                numNet4Projects++;
            }
            else if (project.IsNet45Project())
            {
                numNet45Projects++;
            }
            else
            {
                numOtherNetProjects++;
            }

            return false;
        }


        public abstract void AnalyzeDocument(IDocument document);


        public abstract void OnAnalysisCompleted();
    }
}

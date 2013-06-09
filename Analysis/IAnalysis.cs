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
    public abstract class IAnalysis
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
        public  int numPhoneProjects;
        public int numPhone8Projects;
        public int numAzureProjects;
        public int numNet4Projects;
        public int numNet45Projects;
        public int numOtherNetProjects;



        public IAnalysis(string appName, string dirName)
        {
            this.appName = appName;
            this.dirName = dirName;
            projects = new List<IProject>();
            projectsBySolutions = new Dictionary<ISolution, List<IProject>>();
        }

        public void loadSolutions()
        {
            isUsingSolutionFiles = true;
            var solutionPaths = Directory.GetFiles(dirName, "*.sln", SearchOption.AllDirectories);
            foreach (string solutionPath in solutionPaths)
            {
                var solution=Roslyn.Services.Solution.Load(solutionPath);
                projectsBySolutions.Add(solution, solution.Projects.ToList());
                numTotalProjects += solution.Projects.Count();
            }

        }
        public void loadProjects()
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


        public void analyze()
        {
            if (isUsingSolutionFiles)
            {
                foreach (ISolution solution in projectsBySolutions.Keys)
                {
                    currentSolution = solution;
                    foreach( IProject project in projectsBySolutions[solution])
                        analyzeProject(project);
                }
            }
            else
            {
                foreach (IProject project in projects)
                    analyzeProject(project);
            }
            OnAnalysisCompleted();
        }

        public void analyzeProject(IProject project)
        {

            if (!project.LanguageServices.Language.Equals("C#"))
                return;

            IEnumerable<IDocument> documents=null;
            try
            {
                detectTarget(project);
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

        private void detectTarget(IProject project)
        {
            if (project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone") || a.Display.Contains("WindowsPhone")))
                numPhoneProjects++;
            if (project.MetadataReferences.Any(a => a.Display.Contains("Windows Phone\\v8")) )
                numPhone8Projects++;
            if (project.MetadataReferences.Any(a => a.Display.Contains("Azure")))
                numAzureProjects++;

            if (project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.0")))
                numNet4Projects++;
            else if (project.MetadataReferences.Any(a => a.Display.Contains("Framework\\v4.5") || a.Display.Contains(".NETCore\\v4.5")))
                numNet45Projects++;
            else
                numOtherNetProjects++;

        }

        public abstract void AnalyzeDocument(IDocument document);


        public abstract void OnAnalysisCompleted();

    }
}

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Analysis;

namespace AnalysisRunner
{
    public class App
    {
        public string Name { get; set; }
        public string AppPath { get; set; }
        public List<Project> Projects { get; set; }

        public void PerformAnalysis()
        {
            var projectPaths = from f in Directory.GetFiles(AppPath, "*.csproj", SearchOption.AllDirectories)
                               let directoryName = Path.GetDirectoryName(f)
                               where !directoryName.Contains(@"\tags") &&
                                     !directoryName.Contains(@"\branches")
                               select f;

            foreach (var projectPath in projectPaths)
            {
                var project = new Project()
                {
                    Path = projectPath,
                    Name = projectPath, // TODO: ProejctPath'inden ismini cikar
                    AnalysisResults = new List<AnalysisResult>()
                };

                project.Analyze();
                Projects.Add(project);
            }
        }
    }
}
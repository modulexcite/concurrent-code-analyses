using NLog;
using Roslyn.Services;
using Utilities;

namespace Analysis
{
    public abstract class AnalysisResultBase
    {
        public string AppName;

        public class GeneralResults
        {
            public int NumTotalProjects;
            public int NumUnanalyzedProjects;
            public int NumPhone7Projects;
            public int NumPhone8Projects;
            public int NumNet4Projects;
            public int NumNet45Projects;
            public int NumOtherNetProjects;
            public int NumTotalSLOC;
        }

        public GeneralResults generalResults { get; set; }



        public AnalysisResultBase(string appName)
        {
            generalResults = new GeneralResults();
            AppName = appName;
        }

        public void AddAnalyzedProject(Enums.ProjectType type)
        {
            switch (type)
            {
                case Enums.ProjectType.WP7:
                    generalResults.NumPhone7Projects++;
                    break;

                case Enums.ProjectType.WP8:
                    generalResults.NumPhone8Projects++;
                    break;

                case Enums.ProjectType.NET4:
                    generalResults.NumNet4Projects++;
                    break;

                case Enums.ProjectType.NET45:
                    generalResults.NumNet45Projects++;
                    break;

                case Enums.ProjectType.NETOther:
                    generalResults.NumOtherNetProjects++;
                    break;
            }
        }

        public void AddUnanalyzedProject()
        {
            generalResults.NumUnanalyzedProjects++;
        }

        public void AddProject()
        {
            generalResults.NumTotalProjects++;
        }

        public void WritePhoneProjects(IProject project)
        {
            Logs.phoneProjectListLog.Info(project.FilePath);
            //if (!hasPhoneProjectInThisSolution)
            Logs.phoneSolutionListLog.Info(project.Solution.FilePath);
            //hasPhoneProjectInThisSolution = true;
        }

        public abstract void WriteSummaryLog();
    }
}
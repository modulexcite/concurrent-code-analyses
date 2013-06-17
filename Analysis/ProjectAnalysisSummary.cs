using System.IO;
using Roslyn.Services;
using Utilities;
using System;

namespace Analysis
{
    public abstract class ProjectAnalysisSummary
    {
        public readonly string AppName;

        protected int NumPhoneProjects;
        protected int NumPhone8Projects;
        protected int NumAzureProjects;
        protected int NumNet4Projects;
        protected int NumNet45Projects;
        protected int NumOtherNetProjects;

        protected int NumUnloadedProjects;
        protected int NumTotalProjects;
        protected int NumUnanalyzedProjects;

        protected ProjectAnalysisSummary(string appName)
        {
            AppName = appName;
        }

        public void AddProject(IProject project)
        {
            NumTotalProjects++;

            if (project.IsWPProject())
            {
                NumPhoneProjects++;
            }

            if (project.IsWP8Project())
            {
                NumPhone8Projects++;
            }

            if (project.IsAzureProject())
            {
                NumAzureProjects++;
            }

            if (project.IsNet40Project())
            {
                NumNet4Projects++;
            }
            else if (project.IsNet45Project())
            {
                NumNet45Projects++;
            }
            else
            {
                NumOtherNetProjects++;
            }
        }

        public void AddUnanalyzedProject()
        {
            NumUnanalyzedProjects++;
        }

        public abstract void WriteResults();

        public void WriteResults(StreamWriter logFileWriter)
        {
            var logText = AppName + "," + NumTotalProjects + "," + NumUnloadedProjects + "," + NumUnanalyzedProjects + "\r\n";

            logFileWriter.Write(logText);
        }
    }
}

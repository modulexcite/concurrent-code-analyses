using Utilities;
using System;

namespace Analysis
{
    public abstract class ProjectAnalysisSummary
    {
        public readonly string AppName;

        public int NumPhoneProjects;
        public int NumPhone8Projects;
        public int NumAzureProjects;
        public int NumNet4Projects;
        public int NumNet45Projects;
        public int NumOtherNetProjects;

        public int NumUnloadedProjects;
        public int NumTotalProjects;
        public int NumUnanalyzedProjects;

        protected ProjectAnalysisSummary(string appName)
        {
            AppName = appName;
        }

        public abstract void WriteResults();
    }
}

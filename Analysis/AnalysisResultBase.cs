using System.IO;
using Roslyn.Services;
using Utilities;
using System;
using System.Configuration;
using NLog;

namespace Analysis
{
    public abstract class AnalysisResultBase
    {
        public readonly string _appName;

        protected static readonly Logger SummaryLog = LogManager.GetLogger("SummaryLog");

        protected int NumPhone7Projects;
        protected int NumPhone8Projects;
        protected int NumNet4Projects;
        protected int NumNet45Projects;
        protected int NumOtherNetProjects;

        protected int NumUnloadedProjects;
        protected int NumTotalProjects;
        protected int NumUnanalyzedProjects;

        public AnalysisResultBase(string appName)
        {
            _appName = appName;
        }

        public void AddProject(IProject project)
        {
            NumTotalProjects++;

            var result = project.IsWindowsPhoneProject();
            if (result == 1)
                NumPhone7Projects++;
            else if (result == 2)
                NumPhone8Projects++;

            if (project.IsNet40Project())
                NumNet4Projects++;
            else if (project.IsNet45Project())
                NumNet45Projects++;
            else
                NumOtherNetProjects++;
        }

        public void AddUnanalyzedProject()
        {
            NumUnanalyzedProjects++;
        }

        public abstract void WriteSummaryLog();
    }
}

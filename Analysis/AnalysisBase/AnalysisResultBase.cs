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
        public enum ProjectType { WP7, WP8, NET4, NET45, NETOther };

        public readonly string _appName;

        protected static readonly Logger SummaryLog = LogManager.GetLogger("SummaryLog");
        protected static readonly Logger phoneProjectListLog = LogManager.GetLogger("PhoneProjectListLog");
        protected static readonly Logger phoneSolutionListLog = LogManager.GetLogger("PhoneSolutionListLog");


        public int NumPhone7Projects;
        public int NumPhone8Projects;
        protected int NumNet4Projects;
        protected int NumNet45Projects;
        protected int NumOtherNetProjects;

        protected int NumUnloadedProjects;
        protected int NumTotalProjects;
        protected int NumUnanalyzedProjects;

        public int NumTotalSLOC;

        public AnalysisResultBase(string appName)
        {
            _appName = appName;
        }

        public ProjectType AddProject(IProject project)
        {
            NumTotalProjects++;

            var result = project.IsWindowsPhoneProject();
            if (result == 1)
            {
                NumPhone7Projects++;
                return ProjectType.WP7;
            }
            else if (result == 2)
            {
                NumPhone8Projects++;
                return ProjectType.WP8;
            }
            else if (project.IsNet40Project())
            {
                NumNet4Projects++;
                return ProjectType.NET4;
            }
            else if (project.IsNet45Project())
            {
                NumNet45Projects++;
                return ProjectType.NET45;
            }
            else
            {
                NumOtherNetProjects++;
                return ProjectType.NETOther;
            }
        }

        public void WritePhoneProjects(IProject project)
        {
            phoneProjectListLog.Info(project.FilePath);
            //if (!hasPhoneProjectInThisSolution)
            phoneSolutionListLog.Info(project.Solution.FilePath);
            //hasPhoneProjectInThisSolution = true;

        }

        public void AddUnanalyzedProject()
        {
            NumUnanalyzedProjects++;
        }

        public abstract void WriteSummaryLog();

        
    }
}
